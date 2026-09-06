using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>
/// One Put(List&lt;PropertyInfo&gt; path, value) on a Provider - a value
/// targeted at a field on a **generated ancestor**, reached by walking a path
/// of relationship fields, for this call only.
///
/// path is [rel1, rel2, ..., targetField]: every element but the last is a
/// relationship that is forced generated, and the last is the field the
/// value lands on.
/// </summary>
public sealed class PathValue
{
    public List<PropertyInfo> Path { get; }

    public PathTargetValue Value { get; }

    private PathValue(List<PropertyInfo> path, PathTargetValue value)
    {
        AssertPath(path);
        this.Path = path;
        this.Value = value;
    }

    public static PathValue OfExpression(List<PropertyInfo> path, IValueExpression expression)
    {
        AssertUsablePath(path);
        return new PathValue(path, PathTargetValue.OfExpression(expression));
    }

    public static PathValue OfContextAware(List<PropertyInfo> path, IContextAwareExpression contextAware)
    {
        AssertUsablePath(path);
        return new PathValue(path, PathTargetValue.OfContextAware(contextAware));
    }

    public static PathValue OfLiteral(List<PropertyInfo> path, object? literal)
    {
        AssertUsablePath(path);
        return new PathValue(path, PathTargetValue.OfLiteral(literal));
    }

    public static PathValue OfRequiredRelationship(List<PropertyInfo> path, IDefaultRelationship relationship)
    {
        AssertUsablePath(path);
        return new PathValue(path, PathTargetValue.OfRequiredRelationship(relationship));
    }

    public static PathValue OfOptionalRelationship(List<PropertyInfo> path, IDefaultRelationship relationship)
    {
        AssertUsablePath(path);
        return new PathValue(path, PathTargetValue.OfOptionalRelationship(relationship));
    }

    /// <summary>The path of relationship fields that must be forced generated to reach the target (path minus the target).</summary>
    public List<PropertyInfo> RelationshipPrefix() => [.. this.Path.Take(this.Path.Count - 1)];

    public PropertyInfo Head() => this.Path[0];

    /// <summary>True when only the target field is left - this ancestor level is where the value lands.</summary>
    public bool IsAtTarget() => this.Path.Count == 1;

    /// <summary>True when the value is itself a relationship.</summary>
    public bool IsRelationshipKind() => this.Value.IsRelationship;

    /// <summary>True when the value is a shared ancestor.</summary>
    public bool IsSharedRelationshipValue() => this.Value.IsSharedRelationship;

    /// <summary>The same value, one relationship deeper (head dropped). Only valid when not IsAtTarget().</summary>
    public PathValue Tail() => new([.. this.Path.Skip(1)], this.Value);

    /// <summary>Land the value on template (call only when IsAtTarget()).</summary>
    public void ApplyTo(MasterTemplate template) => this.Value.ApplyTo(template, this.Path[0]);

    private static void AssertPath(List<PropertyInfo>? path)
    {
        if (path is null || path.Count == 0)
        {
            throw new XftyConfigurationException("A path value cannot have an empty path.");
        }

        if (path.Any(step => step is null))
        {
            throw new XftyConfigurationException("A path value cannot contain a null field.");
        }
    }

    /// <summary>The public Put(path, ...) entry points need a relationship step plus a target.</summary>
    private static void AssertUsablePath(List<PropertyInfo>? path)
    {
        if (path is null || path.Count < 2)
        {
            throw new XftyConfigurationException(
                "A path value needs at least one relationship field plus the target field - use plain "
                + "Put(field, value) for a field on the record itself.");
        }
    }
}
