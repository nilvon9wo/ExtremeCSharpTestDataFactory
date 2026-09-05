using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core.Relationships;

/// <summary>One field configuration queued on a <see cref="SharedAncestorProvider"/>, applied once its Master Template is resolved.</summary>
public sealed class SharedAncestorFieldValue
{
    public PropertyInfo Field { get; }

    public object? Value { get; }

    public SharedAncestorFieldValue(PropertyInfo field, object? value)
    {
        this.Field = field;
        this.Value = value;
    }
}
