using System.Reflection;

namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>records[ChildIndex].Field should end up pointing at records[ParentIndex]. Also used by <see cref="DeferredGraph"/> - the same link shape either way.</summary>
public sealed class DepthBatchedInserterParentLink
{
    public int ChildIndex { get; }

    public int ParentIndex { get; }

    public PropertyInfo Field { get; }

    public DepthBatchedInserterParentLink(int childIndex, int parentIndex, PropertyInfo field)
    {
        this.ChildIndex = childIndex;
        this.ParentIndex = parentIndex;
        this.Field = field;
    }
}
