using System.Reflection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Net.Nowhereatall.Xfty.Persistence;
using Qdrant.Client;

namespace Net.Nowhereatall.Xfty.VectorDatabases.Qdrant;

/// <summary>
/// PREVIEW / proof-of-concept - see this package's README for the full list
/// of known assumptions and accepted risks before relying on this in a real
/// test suite.
///
/// An <see cref="IPersistenceGateway"/> that inserts XFTY-generated records
/// into a real Qdrant collection via Microsoft.Extensions.VectorData's
/// dynamic (<c>Dictionary&lt;string, object?&gt;</c>) mapping - chosen
/// specifically because it needs no compile-time-known record type, which
/// matches how every other XFTY gateway already treats records purely
/// through reflection. One collection per distinct record type in the
/// batch, named after the type; the collection is created if missing, using
/// a schema built from the record's own properties.
/// </summary>
public sealed class QdrantPersistenceGateway(QdrantClient client) : IPersistenceGateway
{
    public void Insert(List<object> records, PropertyInfo idField)
    {
        QdrantVectorStore vectorStore = new(client, ownsClient: false);
        records
            .GroupBy(record => record.GetType())
            .ToList()
            .ForEach(group => InsertGroup(vectorStore, [.. group], idField));
    }

    private static void InsertGroup(QdrantVectorStore vectorStore, List<object> records, PropertyInfo idField)
    {
        QdrantRecordReflection.RequireGuidKey(idField);
        records.ForEach(record => QdrantRecordReflection.FillIdIfMissing(record, idField));

        Type recordType = records[0].GetType();
        PropertyInfo vectorField = QdrantRecordReflection.FindVectorField(recordType);
        VectorStoreCollectionDefinition definition = BuildDefinition(recordType, idField, vectorField, records[0]);

        VectorStoreCollection<object, Dictionary<string, object?>> collection =
            vectorStore.GetDynamicCollection(recordType.Name, definition);

        collection.EnsureCollectionExistsAsync().GetAwaiter().GetResult();
        List<Dictionary<string, object?>> rows = [.. records.Select(record => ToRow(record, recordType))];
        collection.UpsertAsync(rows).GetAwaiter().GetResult();
    }

    private static VectorStoreCollectionDefinition BuildDefinition(
        Type recordType, PropertyInfo idField, PropertyInfo vectorField, object sampleRecord)
    {
        int dimensions = ((float[])vectorField.GetValue(sampleRecord)!).Length;
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
                new VectorStoreKeyProperty(idField.Name, typeof(Guid)),
                new VectorStoreVectorProperty(vectorField.Name, vectorField.PropertyType, dimensions),
                .. dataProperties,
            ],
        };
    }

    private static Dictionary<string, object?> ToRow(object record, Type recordType) =>
        recordType.GetProperties().ToDictionary(property => property.Name, property => property.GetValue(record));
}
