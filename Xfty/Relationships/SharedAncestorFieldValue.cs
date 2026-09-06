using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>One field configuration queued on a <see cref="SharedAncestorProvider"/>, applied once its Master Template is resolved.</summary>
public sealed class SharedAncestorFieldValue(PropertyInfo field, object? value)
{
    public PropertyInfo Field { get; } = field;

    public object? Value { get; } = value;
}
