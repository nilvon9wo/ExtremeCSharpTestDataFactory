using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;

namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>
/// Resolves the shared ancestors configured in the current test method,
/// before the main graph build: nested shared ancestors first, each built in
/// memory, then persisted one dependency layer at a time. Cycles throw.
///
/// Works from <see cref="SharedAncestorProvider"/> - the single recipe type -
/// so it never branches on how an ancestor was configured.
///
/// <see cref="ResolveAllConfigured"/> and <see cref="Resolve"/> are
/// the two entry points every path that can trigger resolution funnels
/// through (directly, or via <see cref="SharedAncestor"/>'s own
/// instance/static ResolveNow), so serializing those two - not
/// scattering locks across SharedAncestor itself - is enough to make the
/// whole subsystem safe under concurrent test execution (xUnit's default;
/// this port's own suite opts out, but a consumer's typically doesn't). One
/// global gate, deliberately, not one per shared-ancestor name: this method
/// already recurses (a shared ancestor generating its own sub-graph can
/// reach a *different*, non-nested shared ancestor through an ordinary
/// child relationship, which calls back into ResolveAllConfigured
/// before the outer call has returned) - per-name locks would not compose
/// safely with nested shared ancestors (one name's template referencing
/// another) without a canonical lock-acquisition order to avoid deadlock,
/// for a benefit that does not matter here: resolution happens once per
/// name, ever, in a process, not under sustained concurrent load.
///
/// A plain lock/Monitor cannot hold across an await at all (a continuation
/// can resume on a different thread, breaking the same-thread reentrancy a
/// classic lock relies on), so the gate here is a SemaphoreSlim (the actual
/// cross-call mutual exclusion, safe to await) paired with an AsyncLocal
/// flag tracking whether the *current logical call chain* already holds it -
/// flowing with async continuations rather than tied to one OS thread, so
/// the same nested-nested-call reentrancy the old lock gave for free still
/// holds without risking a chain deadlocking on a gate it's already inside.
/// </summary>
/// <remarks>mode is the triggering call's insert mode; Deferred resolves eagerly, as Now.</remarks>
public sealed class SharedAncestorResolver(IProviderLookup lookup, InsertMode mode)
{
    private static readonly SemaphoreSlim ResolutionGate = new(1, 1);
    private static readonly AsyncLocal<bool> HoldsGate = new();
    private static bool _running;
    private static readonly HashSet<string> InProgress = [];

    private readonly IProviderLookup lookup = lookup;
    private readonly InsertMode mode = Eager(mode);

    /// <summary>Every shared ancestor configured this test method, resolved against the triggering call's mode.</summary>
    public static Task ResolveAllConfigured(IProviderLookup lookup, InsertMode callMode) =>
        WithGate(() => ResolveAllConfiguredUnderGate(lookup, callMode));

    private static async Task ResolveAllConfiguredUnderGate(IProviderLookup lookup, InsertMode callMode)
    {
        if (_running)
        {
            return;
        }

        ApplyLookupDefaults(lookup);
        if (SharedAncestor.IsManualResolutionOnly())
        {
            return;
        }

        List<SharedAncestor> configured = SharedAncestor.ConfiguredUnresolved();
        if (configured.Count > 0)
        {
            await new SharedAncestorResolver(lookup, callMode).Resolve(configured);
        }
    }

    /// <summary>Let a lookup that implements ISharedAncestorDefaults register its shared-ancestor defaults.</summary>
    public static void ApplyLookupDefaults(IProviderLookup lookup)
    {
        if (lookup is ISharedAncestorDefaults defaults)
        {
            defaults.RegisterSharedAncestorDefaults();
        }
    }

    public Task Resolve(List<SharedAncestor> ancestors) => WithGate(() => this.ResolveUnderGate(ancestors));

    private async Task ResolveUnderGate(List<SharedAncestor> ancestors)
    {
        bool owns = !_running;
        _running = true;
        try
        {
            List<SharedAncestor> toResolve = [.. this.InDependencyOrder(ancestors).Where(ancestor => !ancestor.IsResolved)];
            await this.ResolveRemaining(toResolve);
        }
        finally
        {
            if (owns)
            {
                _running = false;
            }
        }
    }

    private async Task ResolveRemaining(List<SharedAncestor> ancestors)
    {
        if (ancestors.Count == 0)
        {
            return;
        }

        await this.ResolveOne(ancestors[0]);
        await this.ResolveRemaining(ancestors.Skip(1).ToList());
    }

    /// <summary>
    /// Runs action while holding the resolution gate - reentrant for the
    /// current async call chain (an already-held gate is recognised via
    /// AsyncLocal and simply runs action directly, no second wait), and
    /// real cross-chain mutual exclusion otherwise, via SemaphoreSlim.
    /// </summary>
    private static async Task WithGate(Func<Task> action)
    {
        if (HoldsGate.Value)
        {
            await action();
            return;
        }

        await ResolutionGate.WaitAsync();
        HoldsGate.Value = true;
        try
        {
            await action();
        }
        finally
        {
            HoldsGate.Value = false;
            _ = ResolutionGate.Release();
        }
    }

    // S0 - collect deepest-first ------------------------------------------

    private List<SharedAncestor> InDependencyOrder(List<SharedAncestor> roots)
    {
        List<SharedAncestor> ordered = [];
        HashSet<string> done = [];
        HashSet<string> onThePath = [];
        roots.ForEach(root => this.Visit(root, ordered, done, onThePath));
        return ordered;
    }

    private void Visit(SharedAncestor ancestor, List<SharedAncestor> ordered, HashSet<string> done, HashSet<string> onThePath)
    {
        string name = ancestor.SharedName;
        if (done.Contains(name))
        {
            return;
        }

        if (ancestor.IsResolved)
        {
            _ = done.Add(name);
            return;
        }

        if (onThePath.Contains(name) || InProgress.Contains(name))
        {
            throw Cycle(name);
        }

        _ = onThePath.Add(name);
        this.NestedOf(ancestor).ForEach(nested => this.Visit(nested, ordered, done, onThePath));
        _ = onThePath.Remove(name);
        _ = done.Add(name);
        ordered.Add(ancestor);
    }

    private List<SharedAncestor> NestedOf(SharedAncestor ancestor)
    {
        MasterTemplate template = ancestor.Source().MasterTemplate(this.lookup);
        return [.. template.RequiredRelationshipByField.Values
            .Concat(template.OptionalRelationshipByField.Values)
            .OfType<SharedAncestor>()];
    }

    // S1 generate + S2 depth-batched persist -----------------------------

    private async Task ResolveOne(SharedAncestor ancestor)
    {
        string name = ancestor.SharedName;
        if (!InProgress.Add(name))
        {
            return;
        }

        try
        {
            await this.BuildAndPersist(ancestor);
        }
        finally
        {
            _ = InProgress.Remove(name);
        }
    }

    private async Task BuildAndPersist(SharedAncestor ancestor)
    {
        SharedAncestorProvider source = ancestor.Source();
        Bundle graph = await source.BuildInMemory(this.lookup);

        DeferredInsertBuffer buffer = new();
        buffer.Add(graph);
        await buffer.ResolveAll(this.mode);

        object record = graph.GetList(source.PrimaryField(this.lookup))![0];
        ancestor.AcceptResolved(record, graph, this.mode == InsertMode.Now);
    }

    private static InsertMode Eager(InsertMode? callMode) =>
        callMode switch
        {
            null or InsertMode.Deferred => InsertMode.Now,
            _ => callMode.Value,
        };

    private static XftyConfigurationException Cycle(string name) =>
        new($"Shared ancestors form a cycle involving \"{name}\". Break it by pre-registering one side with "
            + $"SharedAncestor.Put(\"{name}\", record).");
}
