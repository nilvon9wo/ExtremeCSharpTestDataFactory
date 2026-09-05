using System.Reflection;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
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
/// than mutating.
/// </summary>
public sealed class GenerationContext
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

    /// <summary>A copy carrying the given persistence gateway (top-level entry point).</summary>
    public GenerationContext WithPersistenceGateway(IPersistenceGateway? gateway) =>
        new(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, gateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex, this.ForcedRelationshipPaths, this.PathValues,
            this.BatchedInsertPending, this.ValueFieldPass, this.CycleGuard);

    /// <summary>A copy carrying the given IncludeOptional(...) paths (top-level entry point).</summary>
    public GenerationContext WithForcedRelationshipPaths(List<List<PropertyInfo>>? paths) =>
        new(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, this.PersistenceGateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex, paths, this.PathValues,
            this.BatchedInsertPending, this.ValueFieldPass, this.CycleGuard);

    /// <summary>A copy with a different inclusivity - used to force an explicitly-requested ancestor fully formed.</summary>
    public GenerationContext WithInclusivity(InsertInclusivity newInclusivity) =>
        new(
            this.ProviderLookup, this.InsertMode, newInclusivity, this.PersistenceGateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex, this.ForcedRelationshipPaths, this.PathValues,
            this.BatchedInsertPending, this.ValueFieldPass, this.CycleGuard);

    /// <summary>A copy carrying the given Put(path, value) overrides, their relationship prefixes folded into the forced paths.</summary>
    public GenerationContext WithPathValues(List<PathValue> pathValues)
    {
        List<List<PropertyInfo>> forced = [.. this.ForcedRelationshipPaths, .. pathValues.Select(pathValue => pathValue.RelationshipPrefix())];
        return new GenerationContext(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, this.PersistenceGateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex, forced, pathValues,
            this.BatchedInsertPending, this.ValueFieldPass, this.CycleGuard);
    }

    /// <summary>A copy whose cycle guard permits repeated Provider keys only if cyclesAllowed. Top-level entry point.</summary>
    public GenerationContext WithAncestorCycleGuard(bool cyclesAllowed) =>
        new(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, this.PersistenceGateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex, this.ForcedRelationshipPaths,
            this.PathValues, this.BatchedInsertPending, this.ValueFieldPass, new AncestorCycleGuard(cyclesAllowed));

    /// <summary>A copy whose cycle guard has descended one level into providerKeyHash.</summary>
    public GenerationContext EnteringProviderFor(string providerKeyHash) =>
        new(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, this.PersistenceGateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex, this.ForcedRelationshipPaths,
            this.PathValues, this.BatchedInsertPending, this.ValueFieldPass, this.CycleGuard.DescendingInto(providerKeyHash));

    /// <summary>A copy marked as a structural build whose records get inserted later, depth-batched.</summary>
    public GenerationContext ForBatchedInsert() =>
        new(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, this.PersistenceGateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex, this.ForcedRelationshipPaths, this.PathValues, true,
            this.ValueFieldPass, this.CycleGuard);

    /// <summary>
    /// The context for generating one level of related (ancestor) records:
    /// RelatedOnly becomes Now, PreventCascade becomes None. Every other
    /// mode/inclusivity is carried through unchanged; the per-record fields
    /// are cleared. Forced-relationship paths do not propagate through this
    /// overload - use ForRelated(field) from the recursion.
    /// </summary>
    public GenerationContext ForRelated() => this.ForRelated(null);

    /// <summary>As ForRelated(), but for the child on relationshipField: only forced paths starting with it are carried, head dropped.</summary>
    public GenerationContext ForRelated(PropertyInfo? relationshipField)
    {
        List<List<PropertyInfo>> childPaths = this.ForcedRelationshipPaths
            .Where(path => relationshipField is not null && path.Count > 1 && path[0] == relationshipField)
            .Select(path => path.Skip(1).ToList())
            .ToList();
        List<PathValue> childPathValues = this.PathValues
            .Where(pathValue => relationshipField is not null && !pathValue.IsAtTarget() && pathValue.Head() == relationshipField)
            .Select(pathValue => pathValue.Tail())
            .ToList();
        InsertMode relatedMode = this.InsertMode == InsertMode.RelatedOnly ? InsertMode.Now : this.InsertMode;
        InsertInclusivity relatedInclusivity = this.Inclusivity == InsertInclusivity.PreventCascade ? InsertInclusivity.None : this.Inclusivity;
        return new GenerationContext(
            this.ProviderLookup, relatedMode, relatedInclusivity, this.PersistenceGateway,
            null, null, -1, childPaths, childPathValues, this.BatchedInsertPending, null, this.CycleGuard);
    }

    /// <summary>The context for evaluating a context-aware value on record (row rowIndex), with bundleSoFar holding everything generated so far.</summary>
    public GenerationContext ForRecord(object record, Bundle bundleSoFar, int rowIndex) =>
        new(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, this.PersistenceGateway,
            record, bundleSoFar, rowIndex, this.ForcedRelationshipPaths, this.PathValues, this.BatchedInsertPending, null,
            this.CycleGuard);

    /// <summary>As ForRecord, narrowed to the one context-aware value field being generated now.</summary>
    public GenerationContext ForValueField(PropertyInfo fieldBeingBuilt, IReadOnlySet<PropertyInfo> pendingContextAwareValues) =>
        new(
            this.ProviderLookup, this.InsertMode, this.Inclusivity, this.PersistenceGateway,
            this.RecordBeingBuilt, this.BundleSoFar, this.RowIndex,
            this.ForcedRelationshipPaths, this.PathValues, this.BatchedInsertPending,
            new ValueFieldPass(fieldBeingBuilt, pendingContextAwareValues), this.CycleGuard);

    /// <summary>
    /// The final value of a sibling field on RecordBeingBuilt, for a
    /// context-aware expression. A returned null means the sibling was
    /// genuinely generated to null.
    ///
    /// Throws when siblingField is itself a context-aware value that has not
    /// been generated yet - the one case where Put(...) order matters - so
    /// the mistake surfaces loudly instead of as a silent wrong null.
    /// </summary>
    public object? SiblingValue(PropertyInfo siblingField) =>
        this.ValueFieldPass switch
        {
            null => throw new XftyConfigurationException(
                $"SiblingValue({siblingField.Name}) can only be read while a context-aware value is being generated."),
            { } pass when pass.PendingContextAwareValues.Contains(siblingField) => throw new XftyConfigurationException(
                $"The context-aware value for {pass.FieldBeingBuilt.Name} reads sibling field {siblingField.Name}, "
                + "which is itself a context-aware value that has not been generated yet. Context-aware values are "
                + $"generated in the order they are put, so .Put({siblingField.Name}, ...) must come before "
                + $".Put({pass.FieldBeingBuilt.Name}, ...)."),
            _ => this.RecordBeingBuilt is null
                ? null
                : siblingField.GetValue(this.RecordBeingBuilt),
        };
}
