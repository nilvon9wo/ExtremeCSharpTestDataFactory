namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>
/// <see cref="IPersistenceGateway.Insert"/> takes one record type at a time;
/// a depth-batched layer can mix several. This groups a mixed layer by each
/// record's own "Id" property (the same convention <see cref="IdMocker"/>
/// uses for its mixed-type overload) and calls the gateway once per type -
/// still one call per type, never one call per record.
/// </summary>
public static class PersistenceGatewayExtensions
{
    /// <summary>
    /// One Insert call per type in records, awaited sequentially - not
    /// in parallel, since a real gateway typically wraps a single
    /// non-thread-safe connection/context (EF Core's DbContext, notably)
    /// that cannot service two concurrent calls.
    /// </summary>
    public static Task InsertMixed(this IPersistenceGateway gateway, List<object> records) =>
        InsertGroups(gateway, [.. records.GroupBy(record => record.GetType())]);

    private static Task InsertGroups(IPersistenceGateway gateway, List<IGrouping<Type, object>> groups) =>
        groups.Count == 0
            ? Task.CompletedTask
            : InsertRemainingGroups(gateway, groups);

    private static async Task InsertRemainingGroups(IPersistenceGateway gateway, List<IGrouping<Type, object>> groups)
    {
        IGrouping<Type, object> group = groups[0];
        await gateway.Insert([.. group], group.Key.GetProperty("Id")!);
        await InsertGroups(gateway, groups.Skip(1).ToList());
    }
}
