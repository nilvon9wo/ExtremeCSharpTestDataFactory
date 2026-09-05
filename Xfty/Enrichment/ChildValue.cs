using System.Reflection;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>A forced scalar on the records of a child collection path reaches downward.</summary>
public sealed class ChildValue
{
    public List<PropertyInfo> Path { get; }

    public object? Value { get; }

    public ChildValue(List<PropertyInfo> path, object? value)
    {
        this.Path = path;
        this.Value = value;
    }

    public List<PropertyInfo> RelationshipPrefix() => this.Path.Take(this.Path.Count - 1).ToList();

    public PropertyInfo TargetField() => this.Path[^1];
}
