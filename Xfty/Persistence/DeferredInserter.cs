using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>
/// The registry behind the DEFERRED insert mode.
///
/// A DEFERRED Provider call generates its graph like Never and registers it
/// here instead of inserting. Flush() then saves everything registered so
/// far - across every SupplyBundle() call - in one depth-batched pass.
/// </summary>
public static class DeferredInserter
{
    private static DeferredInsertBuffer _buffer = new();

    public static void Register(Bundle bundle) => _buffer.Add(bundle);

    public static int PendingCount() => _buffer.PendingCount();

    /// <summary>
    /// Save every registered record through <paramref name="gateway"/>,
    /// back-fill its Id, and clear the registry. Throws
    /// <see cref="NotSupportedException"/> if no gateway is supplied - the
    /// registry only clears after a successful save, so a failed Flush()
    /// never silently loses what was registered.
    /// </summary>
    public static void Flush(IPersistenceGateway? gateway = null)
    {
        _buffer.InsertAll(gateway);
        _buffer = new DeferredInsertBuffer();
    }
}
