using System.Reflection;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The state that every step of a single generation run needs to see: which
/// Provider Lookup to resolve variants through, whether (and when) to insert,
/// how far to follow relationships, and - during the context-aware value
/// pass - the record currently being built and the graph generated so far.
///
/// RecordBeingBuilt/BundleSoFar/RowIndex are populated only for the
/// per-record value pass (via ForRecord); everywhere else they are null/-1.
///
/// Immutable. Derive a new one with ForRelated/ForRecord/ForValueField rather
/// than mutating - the With*/For*/Entering* derivation methods live in
/// GenerationContext.Derive.cs; SiblingValue in GenerationContext.Values.cs.
/// </summary>
public sealed partial class GenerationContext
{
    public IProviderLookup ProviderLookup { get; }

    public InsertMode InsertMode { get; }

    public InsertInclusivity Inclusivity { get; }

    /// <summary>The real backing store for InsertMode.Now, if one is configured. Null throws at the point of use.</summary>
    public IPersistenceGateway? PersistenceGateway { get; }

    /// <summary>The record whose value is being generated - only set during the context-aware pass.</summary>
    public object? RecordBeingBuilt { get; }

    /// <summary>The bundle this CreateBundle call has produced so far.</summary>
    public Bundle? BundleSoFar { get; }

    /// <summary>Which row of a multi-record generation RecordBeingBuilt is.</summary>
    public int RowIndex { get; }

    /// <summary>
    /// The single value field whose context-aware expression is running, and
    /// which sibling context-aware values are still ungenerated. Set only
    /// during the context-aware value pass; null everywhere else.
    /// </summary>
    public ValueFieldPass? ValueFieldPass { get; }

    /// <summary>IncludeOptional(...) paths still to apply, each [relationshipField, ...deeper fields].</summary>
    public List<List<PropertyInfo>> ForcedRelationshipPaths { get; }

    /// <summary>Put(path, value) overrides still to apply.</summary>
    public List<PathValue> PathValues { get; }

    /// <summary>True on a structural build whose records get inserted later, depth-batched.</summary>
    public bool BatchedInsertPending { get; }

    /// <summary>The Provider keys currently being generated up the ancestor chain.</summary>
    public AncestorCycleGuard CycleGuard { get; }

    public GenerationContext(IProviderLookup providerLookup, InsertMode? insertMode, InsertInclusivity? inclusivity)
        : this(
            providerLookup, insertMode, inclusivity, null, null, null, -1,
            [], [], false, null, new AncestorCycleGuard(cyclesAllowed: false))
    {
    }

    private GenerationContext(
        IProviderLookup providerLookup,
        InsertMode? insertMode,
        InsertInclusivity? inclusivity,
        IPersistenceGateway? persistenceGateway,
        object? recordBeingBuilt,
        Bundle? bundleSoFar,
        int rowIndex,
        List<List<PropertyInfo>>? forcedRelationshipPaths,
        List<PathValue>? pathValues,
        bool batchedInsertPending,
        ValueFieldPass? valueFieldPass,
        AncestorCycleGuard cycleGuard)
    {
        this.ProviderLookup = providerLookup ?? throw new XftyConfigurationException("A generation context requires a Provider Lookup.");
        this.InsertMode = insertMode ?? InsertMode.Never;
        this.Inclusivity = inclusivity ?? InsertInclusivity.None;
        this.PersistenceGateway = persistenceGateway;
        this.RecordBeingBuilt = recordBeingBuilt;
        this.BundleSoFar = bundleSoFar;
        this.RowIndex = rowIndex;
        this.ForcedRelationshipPaths = forcedRelationshipPaths ?? [];
        this.PathValues = pathValues ?? [];
        this.BatchedInsertPending = batchedInsertPending;
        this.ValueFieldPass = valueFieldPass;
        this.CycleGuard = cycleGuard;
    }
}
