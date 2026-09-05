using System.Reflection;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>A forced scalar on a record several relationship hops up. Path is the hops then the target field.</summary>
public sealed class AncestorValue
{
    public List<PropertyInfo> Path { get; }

    public object? Value { get; }

    public AncestorValue(List<PropertyInfo> path, object? value)
    {
        this.Path = path;
        this.Value = value;
    }

    public List<PropertyInfo> RelationshipPrefix() => [.. this.Path.Take(this.Path.Count - 1)];

    public PropertyInfo TargetField() => this.Path[^1];
}
