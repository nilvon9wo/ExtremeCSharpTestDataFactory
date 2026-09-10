using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>
/// Assigns a placeholder identifier to records before (or instead of) a real
/// insert - <see cref="Core.InsertMode.Mock"/>'s whole job, and useful for
/// pure in-memory unit tests generally, since they never get a real
/// identity-column round-trip. The value's shape is the
/// <see cref="IMockIdGenerator"/>'s call; <see cref="DefaultMockIdGenerator"/>
/// is used when none is supplied.
/// </summary>
public static class IdMocker
{
    public static List<object> AddIds(List<object> records, PropertyInfo idField) =>
        AddIds(records, idField, DefaultMockIdGenerator.Instance);

    public static List<object> AddIds(List<object> records, PropertyInfo idField, IMockIdGenerator generator)
    {
        records.ForEach(record => AddId(record, idField, generator));
        return records;
    }

    /// <summary>As <see cref="AddIds(List{object},PropertyInfo)"/>, for a batch mixing several record types - each record's own Id property is resolved by reflection.</summary>
    public static List<object> AddIds(List<object> records) =>
        AddIds(records, DefaultMockIdGenerator.Instance);

    public static List<object> AddIds(List<object> records, IMockIdGenerator generator)
    {
        records.ForEach(record => AddId(record, record.GetType().GetProperty("Id")!, generator));
        return records;
    }

    public static object AddId(object record, PropertyInfo idField) =>
        AddId(record, idField, DefaultMockIdGenerator.Instance);

    public static object AddId(object record, PropertyInfo idField, IMockIdGenerator generator)
    {
        idField.SetValue(record, generator.NextId(new MockIdContext(record.GetType(), idField, record)));
        return record;
    }

    /// <summary>A bare <c>"mock-N"</c> string, for a caller that just needs a placeholder Id value and not a whole record populated.</summary>
    public static string GenerateId() => DefaultMockIdGenerator.NextMockString();
}
