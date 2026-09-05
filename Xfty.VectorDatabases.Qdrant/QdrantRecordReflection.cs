using System.Reflection;

namespace Net.Nowhereatall.Xfty.VectorDatabases.Qdrant;

/// <summary>
/// Reflection helpers shared by both PoC gateways in this package
/// (<see cref="QdrantPersistenceGateway"/> via Microsoft.Extensions.VectorData,
/// <see cref="QdrantDirectPersistenceGateway"/> via the raw Qdrant client) -
/// the id/vector-field assumptions are the same regardless of which client
/// API actually talks to Qdrant. See README.md for why these are
/// PoC-level assumptions, not general-purpose conventions.
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
