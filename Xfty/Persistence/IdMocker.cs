using System.Reflection;

namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>
/// Assigns a placeholder identifier to records before (or instead of) a real
/// insert - Mock insert mode's whole job, and useful for pure in-memory unit
/// tests generally, since they never get a real identity-column round-trip.
/// The generated value is a simple unique string; nothing downstream parses
/// its format.
/// </summary>
public static class IdMocker
{
    private static int fakeCount;

    public static List<object> AddIds(List<object> records, PropertyInfo idField)
    {
        records.ForEach(record => AddId(record, idField));
        return records;
    }

    /// <summary>As <see cref="AddIds(List{object},PropertyInfo)"/>, for a batch mixing several record types - each record's own Id property is resolved by reflection.</summary>
    public static List<object> AddIds(List<object> records)
    {
        records.ForEach(record => AddId(record, record.GetType().GetProperty("Id")!));
        return records;
    }

    public static object AddId(object record, PropertyInfo idField)
    {
        idField.SetValue(record, GenerateId());
        return record;
    }

    public static string GenerateId() =>
        $"mock-{++fakeCount}";
}
