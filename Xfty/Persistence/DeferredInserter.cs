using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.Persistence;

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

    /// <summary>
    /// Register bundle for the eventual Flush(). excludePrimaryIds marks
    /// only bundle's own top-level primary record(s) - never anything it
    /// pulled in as an ancestor - as never to be given an Id even once
    /// Flush() runs, so a not-yet-inserted record can still relate to
    /// ancestors this same registry resolves for real, efficiently,
    /// alongside everything else registered before the flush.
    /// </summary>
    public static void Register(Bundle bundle, bool excludePrimaryIds = false) => _buffer.Add(bundle, excludePrimaryIds);

    public static int PendingCount() => _buffer.PendingCount();

    /// <summary>
    /// Save every registered record through <paramref name="gateway"/>,
    /// back-fill its Id, and clear the registry. Throws
    /// <see cref="NotSupportedException"/> if no gateway is supplied - the
    /// registry only clears after a successful save, so a failed Flush()
    /// never silently loses what was registered.
    /// </summary>
    public static async Task Flush(IPersistenceGateway? gateway = null)
    {
        await _buffer.InsertAll(gateway);
        _buffer = new DeferredInsertBuffer();
    }

    /// <summary>
    /// Test hygiene only: clears every registered record without inserting
    /// anything. A test proving Flush() throws without a gateway
    /// deliberately leaves the registry non-empty - by design, the same
    /// design that makes a real failed Flush() never silently lose what was
    /// registered - so a test doing that must call this afterward, or every
    /// later test sharing this static registry inherits its leftovers.
    /// </summary>
    public static void ResetForTesting() => _buffer = new DeferredInsertBuffer();
}
