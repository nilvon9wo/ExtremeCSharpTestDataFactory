using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The whole in-memory forest a DEFERRED flush has collected - every
/// generated record and every parent link - presented for an
/// <see cref="Values.IDeferredExpression"/> to read a value up from a
/// descendant.
/// </summary>
public sealed class DeferredGraph
{
    private readonly List<object> records;
    private readonly List<DeferredGraphParentLink> links;

    public DeferredGraph(List<object> records, List<DeferredGraphParentLink> links)
    {
        this.records = records;
        this.links = links;
    }

    /// <summary>The generated records that reference records[parentIndex] through childLookupField.</summary>
    public List<object> ChildrenOf(int parentIndex, PropertyInfo childLookupField) =>
        this.links
            .Where(link => link.ParentIndex == parentIndex && link.Field == childLookupField)
            .Select(link => this.records[link.ChildIndex])
            .ToList();
}
