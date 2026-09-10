using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// The up-flow value pass: runs over the whole DEFERRED forest, just before
/// the depth-batched insert, and fills every field an IDeferredExpression
/// left unresolved by reading it from that record's generated descendants.
/// </summary>
public sealed class DescendantValuePass(
    List<object> records,
    List<DepthBatchedInserterParentLink> links,
    List<PendingDeferredValue> pending
)
{
    private readonly List<object> _records = records;
    private readonly DeferredGraph _graph = new(records, links);
    private readonly List<PendingDeferredValue> _pending = pending;

    public void Complete() => this._pending.ForEach(this.Fill);

    private void Fill(PendingDeferredValue value)
    {
        object target = this._records[value.RecordIndex];
        if (!FieldState.IsUnset(value.Field, target))
        {
            return;
        }

        value.Field.SetValue(target, value.Strategy.Get(this._graph, value.RecordIndex));
    }
}