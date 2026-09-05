using System.Reflection;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>A forced scalar on a record several relationship hops up. Path is the hops then the target field.</summary>
public sealed class AncestorValue(List<PropertyInfo> path, object? value)
{
    public List<PropertyInfo> Path { get; } = path;

    public object? Value { get; } = value;

    public List<PropertyInfo> RelationshipPrefix() => [.. this.Path.Take(this.Path.Count - 1)];

    public PropertyInfo TargetField() => this.Path[^1];
}
