using System.Reflection;

namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>records[ChildIndex].Field should end up pointing at records[ParentIndex]. Also used by <see cref="DeferredGraph"/> - the same link shape either way.</summary>
public sealed class DepthBatchedInserterParentLink(int childIndex, int parentIndex, PropertyInfo field)
{
    public int ChildIndex { get; } = childIndex;

    public int ParentIndex { get; } = parentIndex;

    public PropertyInfo Field { get; } = field;
}
