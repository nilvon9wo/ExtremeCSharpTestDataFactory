using System.Reflection;
using Microsoft.Extensions.VectorData;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.MicrosoftExtensionsVectorData;

/// <summary>
/// PREVIEW / proof-of-concept - see this package's README for the full list
/// of known assumptions and accepted risks before relying on this in a real
/// test suite.
///
/// An <see cref="IPersistenceGateway"/> that inserts XFTY-generated records
/// into any <see cref="VectorStore"/> Microsoft.Extensions.VectorData has a
/// connector for (Qdrant, Redis, Azure AI Search, pgvector, and more) - this
/// class has no dependency on, or knowledge of, which one. It uses MEVD's
/// dynamic (<c>Dictionary&lt;string, object?&gt;</c>) mapping, because
/// that's the one mapping style needing no attributes on the record class
/// and no generic type parameter per record type - the same reflection-only
/// relationship every other part of XFTY has with the record types it
/// generates. Deliberately does not pre-validate id or vector field types
/// the way <c>Xfty.VectorDatabases.Qdrant</c>'s gateway does - those rules
/// vary per backing provider, and baking in one provider's rule here would
/// be wrong for every other one this class is supposed to work with. Errors
/// from an unsupported field shape surface from whatever concrete
/// <see cref="VectorStore"/> is plugged in, not from this class.
/// </summary>
public sealed class MevdPersistenceGateway(VectorStore vectorStore) : IPersistenceGateway
{
    public Task Insert(List<object> records, PropertyInfo idField) =>
        InsertGroups(this, [.. records.GroupBy(record => record.GetType())], idField);

    private static Task InsertGroups(MevdPersistenceGateway gateway, List<IGrouping<Type, object>> groups, PropertyInfo idField) =>
        groups.Count == 0
            ? Task.CompletedTask
            : InsertRemainingGroups(gateway, groups, idField);

    private static async Task InsertRemainingGroups(MevdPersistenceGateway gateway, List<IGrouping<Type, object>> groups, PropertyInfo idField)
    {
        await gateway.InsertGroup([.. groups[0]], idField);
        await InsertGroups(gateway, groups.Skip(1).ToList(), idField);
    }

    private async Task InsertGroup(List<object> records, PropertyInfo idField)
    {
        records.ForEach(record => FillIdIfMissing(record, idField));

        Type recordType = records[0].GetType();
        PropertyInfo vectorField = FindVectorField(recordType);
        VectorStoreCollectionDefinition definition = BuildDefinition(recordType, idField, vectorField, records[0]);

        VectorStoreCollection<object, Dictionary<string, object?>> collection =
            vectorStore.GetDynamicCollection(recordType.Name, definition);

        await collection.EnsureCollectionExistsAsync();
        List<Dictionary<string, object?>> rows = [.. records.Select(record => ToRow(record, recordType))];
        await collection.UpsertAsync(rows);
    }

    private static void FillIdIfMissing(object record, PropertyInfo idField)
    {
        if (idField.GetValue(record) is null)
        {
            idField.SetValue(record, GenerateId(idField.PropertyType));
        }
    }

    private static object GenerateId(Type idType)
    {
        Type underlyingType = Nullable.GetUnderlyingType(idType) ?? idType;
        return underlyingType == typeof(Guid)
            ? Guid.NewGuid()
            : GenerateStringOrFail(underlyingType);
    }

    private static object GenerateStringOrFail(Type underlyingType) =>
        underlyingType == typeof(string)
            ? Guid.NewGuid().ToString()
            : throw new NotSupportedException(
                $"This PoC can only auto-generate a Guid or string id for a field left unset - "
                + $"'{underlyingType.Name}' needs to be set by the Provider template itself. See README.md.");

    private static PropertyInfo FindVectorField(Type recordType) =>
        recordType.GetProperties().First(property => property.PropertyType == typeof(float[]));

    private static VectorStoreCollectionDefinition BuildDefinition(
        Type recordType, PropertyInfo idField, PropertyInfo vectorField, object sampleRecord)
    {
        int dimensions = ((float[])vectorField.GetValue(sampleRecord)!).Length;
        Type keyType = Nullable.GetUnderlyingType(idField.PropertyType) ?? idField.PropertyType;
        List<VectorStoreProperty> dataProperties =
        [
            .. recordType.GetProperties()
                .Where(property => property != idField && property != vectorField)
                .Select(property => new VectorStoreDataProperty(property.Name, property.PropertyType)),
        ];

        return new VectorStoreCollectionDefinition
        {
            Properties =
            [
                new VectorStoreKeyProperty(idField.Name, keyType),
                new VectorStoreVectorProperty(vectorField.Name, vectorField.PropertyType, dimensions),
                .. dataProperties,
            ],
        };
    }

    private static Dictionary<string, object?> ToRow(object record, Type recordType) =>
        recordType.GetProperties().ToDictionary(property => property.Name, property => property.GetValue(record));
}
