using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>
/// Inserts a set of not-yet-persisted records - any mix of record types - in
/// one batch per dependency layer, pointing each child's lookup at its
/// parent's new Id as the layer above it lands.
///
/// InsertAll/ResolveAll are all-or-none. Records are addressed by their
/// index in the list: two records can be equal by value, so an index is the
/// only stable handle on one.
///
/// Apex's SeedAll (best-effort org seeding via Database.insert(_, false)) is
/// not ported - seeding is a deliberate dead end for this port (see
/// csharp-port-idea.md).
/// </summary>
public sealed class DepthBatchedInserter
{
    private readonly List<List<DepthBatchedInserterParentLink>> linksByChild;
    private readonly List<object> records;
    private readonly InsertMode mode;

    private DepthBatchedInserter(List<object> records, List<DepthBatchedInserterParentLink>? links, InsertMode mode)
    {
        this.records = records;
        this.mode = mode;
        this.linksByChild = GroupLinksByChild(records.Count, links);
    }

    /// <summary>Depth-batched real DML.</summary>
    public static void InsertAll(List<object> records, List<DepthBatchedInserterParentLink>? links) =>
        ResolveAll(records, links, InsertMode.Now);

    /// <summary>
    /// Depth-batched resolution honouring the mode: Now inserts each depth
    /// layer, Mock gives it mock Ids - either way the child lookups are
    /// pointed at the layer above as it lands. Never does nothing.
    /// </summary>
    public static void ResolveAll(List<object> records, List<DepthBatchedInserterParentLink>? links, InsertMode mode)
    {
        bool nothingToDo = records.Count == 0 || mode == InsertMode.Never;
        if (nothingToDo)
        {
            return;
        }

        new DepthBatchedInserter(records, links, mode).InsertLayerByLayer();
    }

    private void InsertLayerByLayer() =>
        this.InsertRemainingLayers([.. Enumerable.Range(0, this.records.Count)]);

    private void InsertRemainingLayers(HashSet<int> unpersisted)
    {
        if (unpersisted.Count == 0)
        {
            return;
        }

        List<int> layer = this.TakeNextLayer(unpersisted);
        this.InsertLayer(layer);
        this.InsertRemainingLayers(unpersisted.Except(layer).ToHashSet());
    }

    private List<int> TakeNextLayer(HashSet<int> unpersisted) =>
        FailIfEmpty(unpersisted.Where(index => this.ParentsPersisted(index, unpersisted)).ToList());

    private bool ParentsPersisted(int child, HashSet<int> unpersisted) =>
        !this.linksByChild[child].Any(link => unpersisted.Contains(link.ParentIndex));

    private void InsertLayer(List<int> indexes)
    {
        indexes.ForEach(this.PointAtParents);
        List<object> layer = indexes
            .Select(index => this.records[index])
            .Where(record => IdOf(record) is null)
            .ToList();
        if (layer.Count == 0)
        {
            return;
        }

        _ = this.mode switch
        {
            InsertMode.Mock => IdMocker.AddIds(layer),
            InsertMode.Now => throw new NotSupportedException(
                "InsertMode.Now needs a real persistence layer (e.g. EF), not wired up yet - use Mock or Never."),
            _ => layer,
        };
    }

    private void PointAtParents(int child) =>
        this.linksByChild[child].ForEach(link => link.Field.SetValue(this.records[child], IdOf(this.records[link.ParentIndex])));

    private static object? IdOf(object record) =>
        record.GetType().GetProperty("Id")?.GetValue(record);

    private static List<int> FailIfEmpty(List<int> layer) =>
        layer.Count > 0
            ? layer
            : throw new CyclicGraphException("record lookups form a cycle - no insert order works");

    private static List<List<DepthBatchedInserterParentLink>> GroupLinksByChild(
        int recordCount,
        List<DepthBatchedInserterParentLink>? links)
    {
        List<List<DepthBatchedInserterParentLink>> byChild = Enumerable.Range(0, recordCount)
            .Select(_ => new List<DepthBatchedInserterParentLink>())
            .ToList();
        (links ?? []).ForEach(link => byChild[link.ChildIndex].Add(link));
        return byChild;
    }
}
