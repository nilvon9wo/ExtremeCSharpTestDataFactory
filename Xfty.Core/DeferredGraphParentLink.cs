using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>One generated parent-child link in a <see cref="DeferredGraph"/>.</summary>
public sealed class DeferredGraphParentLink
{
    public int ParentIndex { get; }

    public int ChildIndex { get; }

    public PropertyInfo Field { get; }

    public DeferredGraphParentLink(int parentIndex, int childIndex, PropertyInfo field)
    {
        this.ParentIndex = parentIndex;
        this.ChildIndex = childIndex;
        this.Field = field;
    }
}
