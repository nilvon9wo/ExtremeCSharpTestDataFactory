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
/// directly - bypassing Microsoft.Extensions.VectorData entirely, unlike
/// <see cref="QdrantPersistenceGateway"/>. Exists specifically to compare
/// the two: this one depends only on Qdrant's own stable client (1.19.0),
/// not on another vendor's still-preview connector package, at the cost of
/// building the point/payload mapping by hand instead of getting it from a
/// shared abstraction.
/// </summary>
public sealed class QdrantDirectPersistenceGateway(QdrantClient client) : IPersistenceGateway
{
    public void Insert(List<object> records, PropertyInfo idField) =>
        records
            .GroupBy(record => record.GetType())
            .ToList()
            .ForEach(group => this.InsertGroup([.. group], idField));

    private void InsertGroup(List<object> records, PropertyInfo idField)
    {
        QdrantRecordReflection.RequireGuidKey(idField);
        records.ForEach(record => QdrantRecordReflection.FillIdIfMissing(record, idField));

        Type recordType = records[0].GetType();
        PropertyInfo vectorField = QdrantRecordReflection.FindVectorField(recordType);
        int dimensions = ((float[])vectorField.GetValue(records[0])!).Length;

        this.EnsureCollectionExists(recordType.Name, dimensions);
        List<PointStruct> points = [.. records.Select(record => ToPoint(record, idField, vectorField))];
        _ = client.UpsertAsync(recordType.Name, points).GetAwaiter().GetResult();
    }

    private void EnsureCollectionExists(string collectionName, int dimensions)
    {
        bool exists = client.CollectionExistsAsync(collectionName).GetAwaiter().GetResult();
        if (!exists)
        {
            VectorParams vectorParams = new() { Size = (ulong)dimensions, Distance = Distance.Cosine };
            client.CreateCollectionAsync(collectionName, vectorParams).GetAwaiter().GetResult();
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
