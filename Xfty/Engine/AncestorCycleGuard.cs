namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// Tracks the Provider lookup keys currently being generated up the ancestor
/// chain, so the ancestor generator can refuse to recurse into one that is
/// already in progress - an infinite A -&gt; A -&gt; A ... cycle.
///
/// Keyed by a lookup key's hash key, so a deliberate deep hierarchy built
/// with distinct per-level Providers - different keys - is not a cycle and
/// recurses freely.
/// </summary>
public sealed class AncestorCycleGuard
{
    private readonly bool _cyclesAllowed;
    private readonly HashSet<string> _providerKeyHashesInProgress;

    public AncestorCycleGuard(bool cyclesAllowed) : this(cyclesAllowed, [])
    {
    }

    private AncestorCycleGuard(bool cyclesAllowed, HashSet<string> providerKeyHashesInProgress)
    {
        this._cyclesAllowed = cyclesAllowed;
        this._providerKeyHashesInProgress = providerKeyHashesInProgress;
    }

    /// <summary>True when descending into providerKeyHash would repeat a key already in progress.</summary>
    public bool WouldCycleOn(string providerKeyHash) =>
        !this._cyclesAllowed && this._providerKeyHashesInProgress.Contains(providerKeyHash);

    /// <summary>A guard for one level deeper, with providerKeyHash added to the chain.</summary>
    public AncestorCycleGuard DescendingInto(string providerKeyHash) =>
        new(this._cyclesAllowed, [.. this._providerKeyHashesInProgress, providerKeyHash]);
}