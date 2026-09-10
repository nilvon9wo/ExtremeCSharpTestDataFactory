using System.Reflection;

using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;

namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>
/// Inserts a set of not-yet-persisted records - any mix of record types - in
/// one batch per dependency layer, pointing each child's lookup at its
/// parent's new Id as the layer above it lands.
///
/// InsertAll/ResolveAll are all-or-none. Records are addressed by their
/// index in the list: two records can be equal by value, so an index is the
/// only stable handle on one. Each record type's primary-key field and
/// mock-Id generator come in as <c>idFieldByType</c> / <c>mockIdGeneratorByType</c>
/// (from the bundle each record was generated in) - nothing here assumes the
/// key is called "Id" or that its mock value is a string; the "Id" reflection
/// and <see cref="DefaultMockIdGenerator"/> are only fallbacks for a
/// hand-built caller that supplies no map.
/// </summary>
public sealed class DepthBatchedInserter
{
    private const string ConventionalIdFieldName = "Id";

    private readonly List<List<DepthBatchedInserterParentLink>> linksByChild;
    private readonly List<object> records;
    private readonly InsertMode mode;
    private readonly IPersistenceGateway? gateway;
    private readonly HashSet<int> excludedIndices;
    private readonly IReadOnlyDictionary<Type, PropertyInfo> idFieldByType;
    private readonly IReadOnlyDictionary<Type, IMockIdGenerator> mockIdGeneratorByType;

    private DepthBatchedInserter(
        List<object> records,
        List<DepthBatchedInserterParentLink>? links,
        InsertMode mode,
        IPersistenceGateway? gateway,
        HashSet<int>? excludedIndices,
        IReadOnlyDictionary<Type, PropertyInfo>? idFieldByType,
        IReadOnlyDictionary<Type, IMockIdGenerator>? mockIdGeneratorByType)
    {
        this.records = records;
        this.mode = mode;
        this.gateway = gateway;
        this.excludedIndices = excludedIndices ?? [];
        this.idFieldByType = idFieldByType ?? new Dictionary<Type, PropertyInfo>();
        this.mockIdGeneratorByType = mockIdGeneratorByType ?? new Dictionary<Type, IMockIdGenerator>();
        this.linksByChild = GroupLinksByChild(records.Count, links);
    }

    /// <summary>Depth-batched real insert, via gateway.</summary>
    public static Task InsertAll(
        List<object> records,
        List<DepthBatchedInserterParentLink>? links,
        IPersistenceGateway? gateway = null,
        HashSet<int>? excludedIndices = null,
        IReadOnlyDictionary<Type, PropertyInfo>? idFieldByType = null,
        IReadOnlyDictionary<Type, IMockIdGenerator>? mockIdGeneratorByType = null) =>
        ResolveAll(records, links, InsertMode.Now, gateway, excludedIndices, idFieldByType, mockIdGeneratorByType);

    /// <summary>
    /// Depth-batched resolution honouring the mode: Now inserts each depth
    /// layer through <paramref name="gateway"/>, Mock gives it mock Ids -
    /// either way the child lookups are pointed at the layer above as it
    /// lands. Never does nothing. excludedIndices never receive an Id no
    /// matter the mode (see DeferredInsertBuffer.Add's excludePrimaryIds) -
    /// still wired to their own resolved parents, and still what unblocks
    /// anything waiting on them, exactly as if they genuinely landed.
    /// </summary>
    public static Task ResolveAll(
        List<object> records,
        List<DepthBatchedInserterParentLink>? links,
        InsertMode mode,
        IPersistenceGateway? gateway = null,
        HashSet<int>? excludedIndices = null,
        IReadOnlyDictionary<Type, PropertyInfo>? idFieldByType = null,
        IReadOnlyDictionary<Type, IMockIdGenerator>? mockIdGeneratorByType = null)
    {
        bool nothingToDo = records.Count == 0 || mode == InsertMode.Never;
        return nothingToDo
            ? Task.CompletedTask
            : new DepthBatchedInserter(records, links, mode, gateway, excludedIndices, idFieldByType, mockIdGeneratorByType)
                .InsertLayerByLayer();
    }

    private Task InsertLayerByLayer() =>
        this.InsertRemainingLayers([.. Enumerable.Range(0, this.records.Count)]);

    private async Task InsertRemainingLayers(HashSet<int> unpersisted)
    {
        if (unpersisted.Count == 0)
        {
            return;
        }

        List<int> layer = this.TakeNextLayer(unpersisted);
        await this.InsertLayer(layer).ConfigureAwait(false);
        await this.InsertRemainingLayers([.. unpersisted.Except(layer)]).ConfigureAwait(false);
    }

    private List<int> TakeNextLayer(HashSet<int> unpersisted) =>
        FailIfEmpty([.. unpersisted.Where(index => this.ParentsPersisted(index, unpersisted))]);

    private bool ParentsPersisted(int child, HashSet<int> unpersisted) =>
        !this.linksByChild[child].Any(link => unpersisted.Contains(link.ParentIndex));

    private Task InsertLayer(List<int> indexes)
    {
        indexes.ForEach(this.PointAtParents);
        List<object> layer = [.. indexes
            .Where(index => !this.excludedIndices.Contains(index))
            .Select(index => this.records[index])
            .Where(this.NeedsAnId)];
        return layer.Count == 0
            ? Task.CompletedTask
            : this.mode switch
            {
                InsertMode.Mock => this.MockIds(layer),
                InsertMode.Now => this.InsertNow(layer),
                _ => Task.CompletedTask,
            };
    }

    private Task MockIds(List<object> layer)
    {
        layer.ForEach(record => IdMocker.AddId(record, this.IdFieldFor(record)!, this.MockIdGeneratorFor(record)));
        return Task.CompletedTask;
    }

    private IMockIdGenerator MockIdGeneratorFor(object record) =>
        this.mockIdGeneratorByType.TryGetValue(record.GetType(), out IMockIdGenerator? generator)
            ? generator
            : DefaultMockIdGenerator.Instance;

    private Task InsertNow(List<object> layer) =>
        this.gateway is null
            ? throw new NotSupportedException(
                "InsertMode.Now needs a persistence gateway - pass one to ResolveAll(...)/InsertAll(...), or "
                + "RecordProvider.SetPersistenceGateway(...) - use Mock or Never when none is configured.")
            : this.gateway.InsertMixed(layer, this.idFieldByType);

    private void PointAtParents(int child) =>
        this.linksByChild[child].ForEach(link => link.Field.SetValue(this.records[child], this.IdOf(this.records[link.ParentIndex])));

    private object? IdOf(object record) => this.IdFieldFor(record)?.GetValue(record);

    private bool NeedsAnId(object record) =>
        this.IdFieldFor(record) is { } idField && FieldState.IsUnset(idField, record);

    private PropertyInfo? IdFieldFor(object record) =>
        this.idFieldByType.TryGetValue(record.GetType(), out PropertyInfo? idField)
            ? idField
            : record.GetType().GetProperty(ConventionalIdFieldName);

    private static List<int> FailIfEmpty(List<int> layer) =>
        layer.Count > 0
            ? layer
            : throw new CyclicGraphException("record lookups form a cycle - no insert order works");

    private static List<List<DepthBatchedInserterParentLink>> GroupLinksByChild(
        int recordCount,
        List<DepthBatchedInserterParentLink>? links)
    {
        List<List<DepthBatchedInserterParentLink>> byChild = [.. Enumerable.Range(0, recordCount).Select(_ => new List<DepthBatchedInserterParentLink>())];
        (links ?? []).ForEach(link => byChild[link.ChildIndex].Add(link));
        return byChild;
    }
}
