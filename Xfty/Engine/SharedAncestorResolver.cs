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
/// <see cref="ResolveAllConfigured"/> and <see cref="Resolve"/> are the two
/// entry points every path that can trigger resolution funnels through
/// (directly, or via <see cref="SharedAncestor"/>'s own instance/static
/// ResolveNow), so serializing those two - not scattering locks across
/// SharedAncestor itself - is enough to make the whole subsystem safe under
/// concurrent test execution (xUnit's default; this port's own suite opts
/// out, but a consumer's typically doesn't). The lock is a plain
/// Monitor-based one, deliberately: this method already recurses on the
/// same thread (resolving one ancestor's sub-graph can itself trigger
/// ordinary record generation that calls back into ResolveAllConfigured),
/// and Monitor's same-thread reentrancy is well-established, unlike newer
/// lock primitives this codebase has never needed to reason about before.
/// </summary>
public sealed class SharedAncestorResolver
{
    private static readonly object ResolutionLock = new();
    private static bool _running;
    private static readonly HashSet<string> InProgress = [];

    private readonly IProviderLookup lookup;
    private readonly InsertMode mode;

    /// <summary>mode is the triggering call's insert mode; Deferred/RelatedOnly resolve eagerly, as Now.</summary>
    public SharedAncestorResolver(IProviderLookup lookup, InsertMode mode)
    {
        this.lookup = lookup;
        this.mode = Eager(mode);
    }

    /// <summary>Every shared ancestor configured this test method, resolved against the triggering call's mode.</summary>
    public static void ResolveAllConfigured(IProviderLookup lookup, InsertMode callMode)
    {
        lock (ResolutionLock)
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
                new SharedAncestorResolver(lookup, callMode).Resolve(configured);
            }
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

    public void Resolve(List<SharedAncestor> ancestors)
    {
        lock (ResolutionLock)
        {
            bool owns = !_running;
            _running = true;
            try
            {
                this.InDependencyOrder(ancestors).Where(ancestor => !ancestor.IsResolved).ToList().ForEach(this.ResolveOne);
            }
            finally
            {
                if (owns)
                {
                    _running = false;
                }
            }
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

    private void ResolveOne(SharedAncestor ancestor)
    {
        string name = ancestor.SharedName;
        if (!InProgress.Add(name))
        {
            return;
        }

        try
        {
            this.BuildAndPersist(ancestor);
        }
        finally
        {
            _ = InProgress.Remove(name);
        }
    }

    private void BuildAndPersist(SharedAncestor ancestor)
    {
        SharedAncestorProvider source = ancestor.Source();
        Bundle graph = source.BuildInMemory(this.lookup);

        DeferredInsertBuffer buffer = new();
        buffer.Add(graph);
        buffer.ResolveAll(this.mode);

        object record = graph.GetList(source.PrimaryField(this.lookup))![0];
        ancestor.AcceptResolved(record, graph, this.mode == InsertMode.Now);
    }

    private static InsertMode Eager(InsertMode? callMode)
    {
        bool resolveEagerly = callMode is null or InsertMode.Deferred or InsertMode.RelatedOnly;
        return resolveEagerly
            ? InsertMode.Now
            : callMode!.Value;
    }

    private static XftyConfigurationException Cycle(string name) =>
        new($"Shared ancestors form a cycle involving \"{name}\". Break it by pre-registering one side with "
            + $"SharedAncestor.Put(\"{name}\", record).");
}
