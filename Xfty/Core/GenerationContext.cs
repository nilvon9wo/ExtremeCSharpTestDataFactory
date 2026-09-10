using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.PathValues;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
namespace Net.NowhereAtAll.Xfty.Core;

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
    public IProviderLookup ProviderLookup { get; init; }

    public InsertMode InsertMode { get; init; }

    public InsertInclusivity Inclusivity { get; init; }

    /// <summary>
    /// The real backing store for InsertMode.Now, if one is configured. Null throws at the point of use.
    /// </summary>
    public IPersistenceGateway? PersistenceGateway { get; init; }

    /// <summary>
    /// The optional collaborator that fills in fields the Master Template never configured. Null: nothing does.
    /// </summary>
    public IUnsetFieldFiller? UnsetFieldFiller { get; init; }

    /// <summary>The record whose value is being generated - only set during the context-aware pass.</summary>
    public object? RecordBeingBuilt { get; init; }

    /// <summary>The bundle this CreateBundle call has produced so far.</summary>
    public Bundle? BundleSoFar { get; init; }

    /// <summary>Which row of a multi-record generation RecordBeingBuilt is.</summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// The single value field whose context-aware expression is running, and
    /// which sibling context-aware values are still ungenerated. Set only
    /// during the context-aware value pass; null everywhere else.
    /// </summary>
    public ValueFieldPass? ValueFieldPass { get; init; }

    /// <summary>IncludeOptional(...) paths still to apply, each [relationshipField, ...deeper fields].</summary>
    public List<List<PropertyInfo>> ForcedRelationshipPaths { get; init; }

    /// <summary>Put(path, value) overrides still to apply.</summary>
    public List<PathValue> PathValues { get; init; }

    /// <summary>True on a structural build whose records get inserted later, depth-batched.</summary>
    public bool BatchedInsertPending { get; init; }

    /// <summary>
    /// True only for the top-level record(s) this call itself is generating -
    /// never for an ancestor, regardless of how it was set. Excludes those
    /// specific records from persistence (Mock-Id assignment or a real
    /// insert) however the rest of the graph is being persisted; see
    /// <see cref="RecordProvider.ExcludePrimaryIds"/>.
    /// </summary>
    public bool ExcludePrimaryIds { get; init; }

    /// <summary>The Provider keys currently being generated up the ancestor chain.</summary>
    public AncestorCycleGuard CycleGuard { get; init; }

    public GenerationContext(
        IProviderLookup providerLookup,
        InsertMode? insertMode,
        InsertInclusivity? inclusivity
    )
    {
        this.ProviderLookup = providerLookup
            ?? throw new XftyConfigurationException(
                "A generation context requires a Provider Lookup."
            );
        this.InsertMode = insertMode ?? InsertMode.Never;
        this.Inclusivity = inclusivity ?? InsertInclusivity.None;
        this.RowIndex = -1;
        this.ForcedRelationshipPaths = [];
        this.PathValues = [];
        this.CycleGuard = new AncestorCycleGuard(cyclesAllowed: false);
    }

    private GenerationContext(GenerationContext source)
    {
        this.ProviderLookup = source.ProviderLookup;
        this.InsertMode = source.InsertMode;
        this.Inclusivity = source.Inclusivity;
        this.PersistenceGateway = source.PersistenceGateway;
        this.UnsetFieldFiller = source.UnsetFieldFiller;
        this.RecordBeingBuilt = source.RecordBeingBuilt;
        this.BundleSoFar = source.BundleSoFar;
        this.RowIndex = source.RowIndex;
        this.ForcedRelationshipPaths = source.ForcedRelationshipPaths;
        this.PathValues = source.PathValues;
        this.BatchedInsertPending = source.BatchedInsertPending;
        this.ExcludePrimaryIds = source.ExcludePrimaryIds;
        this.ValueFieldPass = source.ValueFieldPass;
        this.CycleGuard = source.CycleGuard;
    }

    /// <summary>A copy carrying the given persistence gateway (top-level entry point).</summary>
    public GenerationContext WithPersistenceGateway(IPersistenceGateway? gateway) =>
        new(this) { PersistenceGateway = gateway };

    /// <summary>A copy carrying the given unset-field filler (top-level entry point).</summary>
    public GenerationContext WithUnsetFieldFiller(IUnsetFieldFiller? filler) =>
        new(this) { UnsetFieldFiller = filler };

    /// <summary>A copy carrying the given IncludeOptional(...) paths (top-level entry point).</summary>
    public GenerationContext WithForcedRelationshipPaths(List<List<PropertyInfo>>? paths) =>
        new(this) { ForcedRelationshipPaths = paths ?? [] };

    /// <summary>
    /// A copy with a different inclusivity - used to force an explicitly-requested ancestor fully formed.
    /// </summary>
    public GenerationContext WithInclusivity(InsertInclusivity newInclusivity) =>
        new(this) { Inclusivity = newInclusivity };

    /// <summary>
    /// A copy carrying the given Put(path, value) overrides, their relationship prefixes folded into the forced paths.
    /// </summary>
    public GenerationContext WithPathValues(List<PathValue> pathValues) =>
        new(this)
        {
            ForcedRelationshipPaths =
                [.. this.ForcedRelationshipPaths, .. pathValues.Select(each => each.RelationshipPrefix())],
            PathValues = pathValues,
        };

    /// <summary>
    /// A copy whose cycle guard permits repeated Provider keys only if cyclesAllowed. Top-level entry point.
    /// </summary>
    public GenerationContext WithAncestorCycleGuard(bool cyclesAllowed) =>
        new(this) { CycleGuard = new AncestorCycleGuard(cyclesAllowed) };

    /// <summary>
    /// A copy carrying whether this call's own primary record(s) are excluded from persistence (top-level entry point).
    /// </summary>
    public GenerationContext WithPrimaryIdsExcluded(bool excluded) =>
        new(this) { ExcludePrimaryIds = excluded };

    /// <summary>A copy whose cycle guard has descended one level into providerKeyHash.</summary>
    public GenerationContext EnteringProviderFor(string providerKeyHash) =>
        new(this) { CycleGuard = this.CycleGuard.DescendingInto(providerKeyHash) };

    /// <summary>A copy marked as a structural build whose records get inserted later, depth-batched.</summary>
    public GenerationContext ForBatchedInsert() =>
        new(this) { BatchedInsertPending = true };

    /// <summary>
    /// The context for generating one level of related (ancestor) records:
    /// PreventCascade becomes None; ExcludePrimaryIds always resets to
    /// false, since that setting means "this call's own primary," never an
    /// ancestor - an ancestor is always persisted the same way it always
    /// was, regardless of what the record referencing it opted out of.
    /// Every other mode/inclusivity is carried through unchanged; the
    /// per-record fields are cleared. Forced-relationship paths do not
    /// propagate through this overload - use ForRelated(field) from the
    /// recursion.
    /// </summary>
    public GenerationContext ForRelated() => this.ForRelated(null);

    /// <summary>
    /// As ForRelated(), but for the child on relationshipField: only forced paths starting with it are carried, head
    /// dropped.
    /// </summary>
    public GenerationContext ForRelated(PropertyInfo? relationshipField)
    {
        List<List<PropertyInfo>> childPaths = [.. this.ForcedRelationshipPaths
            .Where(path => relationshipField is not null && path.Count > 1 && path[0] == relationshipField)
            .Select(path => path.Skip(1).ToList())];
        List<PathValue> childPathValues = [.. this.PathValues
            .Where(each =>
                relationshipField is not null
                && !each.IsAtTarget()
                && each.Head() == relationshipField)
            .Select(each => each.Tail())];
        return new GenerationContext(this)
        {
            Inclusivity =
                this.Inclusivity == InsertInclusivity.PreventCascade
                    ? InsertInclusivity.None
                    : this.Inclusivity,
            RecordBeingBuilt = null,
            BundleSoFar = null,
            RowIndex = -1,
            ForcedRelationshipPaths = childPaths,
            PathValues = childPathValues,
            ExcludePrimaryIds = false,
            ValueFieldPass = null,
        };
    }

    /// <summary>
    /// The context for evaluating a context-aware value on record (row rowIndex), with bundleSoFar holding everything
    /// generated so far.
    /// </summary>
    public GenerationContext ForRecord(object record, Bundle bundleSoFar, int rowIndex) =>
        new(this)
        {
            RecordBeingBuilt = record,
            BundleSoFar = bundleSoFar,
            RowIndex = rowIndex,
            ValueFieldPass = null,
        };

    /// <summary>As ForRecord, narrowed to the one context-aware value field being generated now.</summary>
    public GenerationContext ForValueField(
        PropertyInfo fieldBeingBuilt,
        IReadOnlyCollection<PropertyInfo> pendingContextAwareValues
    ) =>
        new(this)
        {
            ValueFieldPass = new ValueFieldPass(fieldBeingBuilt, pendingContextAwareValues),
        };

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