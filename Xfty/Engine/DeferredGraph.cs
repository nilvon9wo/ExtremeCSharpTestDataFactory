using System.Reflection;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// The whole in-memory forest a DEFERRED flush has collected - every
/// generated record and every parent link - presented for an
/// <see cref="Values.IDeferredExpression"/> to read a value up from a
/// descendant.
/// </summary>
public sealed class DeferredGraph(List<object> records, List<DepthBatchedInserterParentLink> links)
{
    private readonly List<object> records = records;
    private readonly List<DepthBatchedInserterParentLink> links = links;

    /// <summary>The generated records that reference records[parentIndex] through childLookupField.</summary>
    public List<object> ChildrenOf(int parentIndex, PropertyInfo childLookupField) =>
        [.. this.ChildIndicesOf(parentIndex, childLookupField).Select(this.RecordAt)];

    /// <summary>
    /// The flat indices of the generated records that reference
    /// records[parentIndex] through childLookupField - lets a caller walk a
    /// second hop (that child's own children) via <see cref="RecordAt"/>,
    /// which <see cref="ChildrenOf"/> alone cannot support.
    /// </summary>
    public List<int> ChildIndicesOf(int parentIndex, PropertyInfo childLookupField) =>
        [.. this.links
            .Where(link => link.ParentIndex == parentIndex && link.Field == childLookupField)
            .Select(link => link.ChildIndex)];

    /// <summary>The generated record at this flat index - pairs with <see cref="ChildIndicesOf"/> for a multi-hop walk.</summary>
    public object RecordAt(int index) => this.records[index];
}
