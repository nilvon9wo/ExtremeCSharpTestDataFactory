using System.Reflection;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>GenerationContext - deriving a copy that differs in exactly one concern, rather than mutating.</summary>
public sealed partial class GenerationContext
{
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
}
