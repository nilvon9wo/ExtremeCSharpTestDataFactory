using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// The up-flow value pass: runs over the whole DEFERRED forest, just before
/// the depth-batched insert, and fills every field an IDeferredExpression
/// left unresolved by reading it from that record's generated descendants.
/// </summary>
public sealed class DescendantValuePass(List<object> records, List<DepthBatchedInserterParentLink> links, List<PendingDeferredValue> pending)
{
    private readonly List<object> records = records;
    private readonly DeferredGraph graph = new(records, links);
    private readonly List<PendingDeferredValue> pending = pending;

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
