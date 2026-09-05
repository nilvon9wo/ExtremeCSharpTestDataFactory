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
    private static DeferredInsertBuffer buffer = new();

    public static void Register(Bundle bundle) => buffer.Add(bundle);

    public static int PendingCount() => buffer.PendingCount();

    /// <summary>Save every registered record, back-fill its Id, and clear the registry.</summary>
    public static void Flush()
    {
        buffer.InsertAll();
        buffer = new DeferredInsertBuffer();
    }
}
