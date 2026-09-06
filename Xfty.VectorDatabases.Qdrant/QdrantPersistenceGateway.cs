using System.Reflection;
using Net.Nowhereatall.Xfty.Persistence;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Net.Nowhereatall.Xfty.VectorDatabases.Qdrant;

/// <summary>
/// PREVIEW / proof-of-concept - see this package's README for the full list
/// of known assumptions and accepted risks before relying on this in a real
/// test suite.
///
/// An <see cref="IPersistenceGateway"/> that inserts XFTY-generated records
/// into a real Qdrant collection through <see cref="QdrantClient"/>
/// directly - no Microsoft.Extensions.VectorData, no Semantic Kernel
/// connector. Depends only on Qdrant's own stable client (1.19.0), at the
/// cost of building the point/payload mapping by hand rather than getting
/// it from a shared abstraction. See
/// <c>Xfty.VectorDatabases.MicrosoftExtensionsVectorData</c>'s
/// <c>MevdPersistenceGateway</c> for the same job through that abstraction
/// instead - kept as a separate package on purpose, not bundled with this
/// one, so depending on this gateway never pulls in MEVD or a
/// Semantic-Kernel-branded connector this class doesn't use.
/// </summary>
public sealed class QdrantPersistenceGateway(QdrantClient client) : IPersistenceGateway
{
    public Task Insert(List<object> records, PropertyInfo idField) =>
        InsertGroups(this, [.. records.GroupBy(record => record.GetType())], idField);

    private static Task InsertGroups(QdrantPersistenceGateway gateway, List<IGrouping<Type, object>> groups, PropertyInfo idField) =>
        groups.Count == 0
            ? Task.CompletedTask
            : InsertRemainingGroups(gateway, groups, idField);

    private static async Task InsertRemainingGroups(QdrantPersistenceGateway gateway, List<IGrouping<Type, object>> groups, PropertyInfo idField)
    {
        await gateway.InsertGroup([.. groups[0]], idField);
        await InsertGroups(gateway, groups.Skip(1).ToList(), idField);
    }

    private async Task InsertGroup(List<object> records, PropertyInfo idField)
    {
        QdrantRecordReflection.RequireGuidKey(idField);
        records.ForEach(record => QdrantRecordReflection.FillIdIfMissing(record, idField));

        Type recordType = records[0].GetType();
        PropertyInfo vectorField = QdrantRecordReflection.FindVectorField(recordType);
        int dimensions = ((float[])vectorField.GetValue(records[0])!).Length;

        await this.EnsureCollectionExists(recordType.Name, dimensions);
        List<PointStruct> points = [.. records.Select(record => ToPoint(record, idField, vectorField))];
        _ = await client.UpsertAsync(recordType.Name, points);
    }

    private async Task EnsureCollectionExists(string collectionName, int dimensions)
    {
        bool exists = await client.CollectionExistsAsync(collectionName);
        if (!exists)
        {
            VectorParams vectorParams = new() { Size = (ulong)dimensions, Distance = Distance.Cosine };
            await client.CreateCollectionAsync(collectionName, vectorParams);
        }
    }

    private static PointStruct ToPoint(object record, PropertyInfo idField, PropertyInfo vectorField)
    {
        PointStruct point = new()
        {
            Id = (Guid)idField.GetValue(record)!,
            Vectors = (float[])vectorField.GetValue(record)!,
        };
        PayloadPropertiesOf(record.GetType(), idField, vectorField)
            .ToList()
            .ForEach(property => SetPayloadValue(point, property, property.GetValue(record)));
        return point;
    }

    private static IEnumerable<PropertyInfo> PayloadPropertiesOf(Type recordType, PropertyInfo idField, PropertyInfo vectorField) =>
        recordType.GetProperties().Where(property => property != idField && property != vectorField);

    private static void SetPayloadValue(PointStruct point, PropertyInfo property, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case string stringValue:
                point.Payload[property.Name] = stringValue;
                break;
            case bool boolValue:
                point.Payload[property.Name] = boolValue;
                break;
            case int or long:
                point.Payload[property.Name] = Convert.ToInt64(value);
                break;
            case float or double:
                point.Payload[property.Name] = Convert.ToDouble(value);
                break;
            default:
                throw new NotSupportedException(
                    $"This PoC's payload mapping only supports string/bool/int/long/float/double - "
                    + $"'{property.Name}' is {property.PropertyType.Name}. See README.md.");
        }
    }
}
