using System.Reflection;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.Qdrant;

/// <summary>
/// Reflection helpers for <see cref="QdrantPersistenceGateway"/> - the
/// id/vector-field assumptions this PoC makes about a record's shape.
/// Note the parallel, deliberately un-shared logic in
/// <c>Xfty.VectorDatabases.MicrosoftExtensionsVectorData</c>'s own gateway:
/// that one does NOT require a `Guid` key, because that's Qdrant's own
/// rule, not a universal one - see README.md for both packages' reasoning.
/// </summary>
internal static class QdrantRecordReflection
{
    internal static void RequireGuidKey(PropertyInfo idField)
    {
        Type underlyingType = Nullable.GetUnderlyingType(idField.PropertyType) ?? idField.PropertyType;
        if (underlyingType != typeof(Guid))
        {
            throw new NotSupportedException(
                $"This PoC only supports a Guid-typed id field - Qdrant's own connector rejects "
                + $"string keys outright (discovered by running this test, not assumed - see README.md). "
                + $"'{idField.Name}' is {idField.PropertyType.Name}.");
        }
    }

    internal static void FillIdIfMissing(object record, PropertyInfo idField)
    {
        if (idField.GetValue(record) is null)
        {
            idField.SetValue(record, Guid.NewGuid());
        }
    }

    internal static PropertyInfo FindVectorField(Type recordType) =>
        recordType.GetProperties().First(property => property.PropertyType == typeof(float[]));
}
