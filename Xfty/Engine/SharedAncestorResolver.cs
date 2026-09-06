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
/// out, but a consumer's typically doesn't). One global lock, deliberately,
/// not one per shared-ancestor name: this method already recurses on the
/// same thread (a shared ancestor generating its own sub-graph can reach a
/// *different*, non-nested shared ancestor through an ordinary child
/// relationship, which calls back into ResolveAllConfigured before the
/// outer call has returned), and <see cref="Lock"/> is reentrant for that
/// same-thread case, same as <see cref="System.Threading.Monitor"/> - but
/// per-name locks would not compose safely with nested shared ancestors
/// (one name's template referencing another) without a canonical
/// lock-acquisition order to avoid deadlock, for a benefit that does not
/// matter here: resolution happens once per name, ever, in a process, not
/// under sustained concurrent load.
/// </summary>
/// <remarks>mode is the triggering call's insert mode; Deferred resolves eagerly, as Now.</remarks>
public sealed class SharedAncestorResolver(IProviderLookup lookup, InsertMode mode)
{
    private static readonly Lock ResolutionLock = new();
    private static bool _running;
    private static readonly HashSet<string> InProgress = [];

    private readonly IProviderLookup lookup = lookup;
    private readonly InsertMode mode = Eager(mode);

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
