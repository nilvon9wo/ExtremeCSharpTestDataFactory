using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Enrichment;

/// <summary>A forced scalar on the records of a child collection path reaches downward.</summary>
public sealed class ChildValue(List<PropertyInfo> path, object? value)
{
    public List<PropertyInfo> Path { get; } = path;

    public object? Value { get; } = value;

    public List<PropertyInfo> RelationshipPrefix() => [.. this.Path.Take(this.Path.Count - 1)];

    public PropertyInfo TargetField() => this.Path[^1];
}
