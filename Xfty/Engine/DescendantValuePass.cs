using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>
/// The up-flow value pass: runs over the whole DEFERRED forest, just before
/// the depth-batched insert, and fills every field an IDeferredExpression
/// left unresolved by reading it from that record's generated descendants.
/// </summary>
public sealed class DescendantValuePass
{
    private readonly List<object> records;
    private readonly DeferredGraph graph;
    private readonly List<PendingDeferredValue> pending;

    public DescendantValuePass(List<object> records, List<DepthBatchedInserterParentLink> links, List<PendingDeferredValue> pending)
    {
        this.records = records;
        this.graph = new DeferredGraph(records, links);
        this.pending = pending;
    }

    public void Complete() => this.pending.ForEach(this.Fill);

    private void Fill(PendingDeferredValue value)
    {
        object target = this.records[value.RecordIndex];
        if (value.Field.GetValue(target) is not null)
        {
            return;
        }

        value.Field.SetValue(target, value.Strategy.Get(this.graph, value.RecordIndex));
    }
}
