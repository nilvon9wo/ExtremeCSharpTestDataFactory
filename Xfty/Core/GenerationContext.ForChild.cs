using System.Reflection;
using Net.Nowhereatall.Xfty.Engine;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>GenerationContext - deriving the context for one level of related (ancestor) records, or one record's value pass.</summary>
public sealed partial class GenerationContext
{
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
        List<List<PropertyInfo>> childPaths = this.ChildPathsUnder(relationshipField);
        List<PathValue> childPathValues = this.ChildPathValuesUnder(relationshipField);
        InsertMode relatedMode = this.InsertMode == InsertMode.RelatedOnly ? InsertMode.Now : this.InsertMode;
        InsertInclusivity relatedInclusivity = this.Inclusivity == InsertInclusivity.PreventCascade ? InsertInclusivity.None : this.Inclusivity;
        return new GenerationContext(
            this.ProviderLookup, relatedMode, relatedInclusivity, this.PersistenceGateway,
            null, null, -1, childPaths, childPathValues, this.BatchedInsertPending, null, this.CycleGuard);
    }

    private List<List<PropertyInfo>> ChildPathsUnder(PropertyInfo? relationshipField) =>
        this.ForcedRelationshipPaths
            .Where(path => relationshipField is not null && path.Count > 1 && path[0] == relationshipField)
            .Select(path => path.Skip(1).ToList())
            .ToList();

    private List<PathValue> ChildPathValuesUnder(PropertyInfo? relationshipField) =>
        this.PathValues
            .Where(pathValue => relationshipField is not null && !pathValue.IsAtTarget() && pathValue.Head() == relationshipField)
            .Select(pathValue => pathValue.Tail())
            .ToList();

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
}
