namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>
/// <see cref="IPersistenceGateway.Insert"/> takes one record type at a time;
/// a depth-batched layer can mix several. This groups a mixed layer by each
/// record's own "Id" property (the same convention <see cref="IdMocker"/>
/// uses for its mixed-type overload) and calls the gateway once per type -
/// still one call per type, never one call per record.
/// </summary>
public static class PersistenceGatewayExtensions
{
    public static void InsertMixed(this IPersistenceGateway gateway, List<object> records) =>
        records
            .GroupBy(record => record.GetType())
            .ToList()
            .ForEach(group => gateway.Insert([.. group], group.Key.GetProperty("Id")!));
}
