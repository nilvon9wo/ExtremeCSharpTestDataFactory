using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>
/// <see cref="IPersistenceGateway.Insert"/> takes one record type at a time;
/// a depth-batched layer can mix several. This groups a mixed layer by record
/// type and calls the gateway once per type - still one call per type, never
/// one call per record. Each type's primary-key field comes from
/// <c>idFieldByType</c> (the bundle each record was generated in); the "Id"
/// reflection is only a fallback for a caller that supplies no map.
/// </summary>
public static class PersistenceGatewayExtensions
{
    private const string ConventionalIdFieldName = "Id";

    /// <summary>
    /// One Insert call per type in records, awaited sequentially - not
    /// in parallel, since a real gateway typically wraps a single
    /// non-thread-safe connection/context (EF Core's DbContext, notably)
    /// that cannot service two concurrent calls.
    /// </summary>
    public static Task InsertMixed(
        this IPersistenceGateway gateway,
        List<object> records,
        IReadOnlyDictionary<Type, PropertyInfo>? idFieldByType = null) =>
        InsertGroups(gateway, [.. records.GroupBy(record => record.GetType())], idFieldByType ?? new Dictionary<Type, PropertyInfo>());

    private static Task InsertGroups(
        IPersistenceGateway gateway,
        List<IGrouping<Type, object>> groups,
        IReadOnlyDictionary<Type, PropertyInfo> idFieldByType) =>
        groups.Count == 0
            ? Task.CompletedTask
            : InsertRemainingGroups(gateway, groups, idFieldByType);

    private static async Task InsertRemainingGroups(
        IPersistenceGateway gateway,
        List<IGrouping<Type, object>> groups,
        IReadOnlyDictionary<Type, PropertyInfo> idFieldByType)
    {
        IGrouping<Type, object> group = groups[0];
        await gateway.Insert([.. group], IdFieldOf(group.Key, idFieldByType)).ConfigureAwait(false);
        await InsertGroups(gateway, groups.Skip(1).ToList(), idFieldByType).ConfigureAwait(false);
    }

    private static PropertyInfo IdFieldOf(Type recordType, IReadOnlyDictionary<Type, PropertyInfo> idFieldByType) =>
        idFieldByType.TryGetValue(recordType, out PropertyInfo? idField)
            ? idField
            : recordType.GetProperty(ConventionalIdFieldName)!;
}
