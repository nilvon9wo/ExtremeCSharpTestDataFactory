using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Relationships;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The value half of a <see cref="PathValue"/> - what lands on the field a
/// Put(path, ...) targets, in one of five kinds.
/// </summary>
public sealed class PathTargetValue
{
    private readonly object? payload;

    public PathTargetValueKind ValueKind { get; }

    private PathTargetValue(PathTargetValueKind kind, object? payload)
    {
        this.ValueKind = kind;
        this.payload = payload;
    }

    public static PathTargetValue OfExpression(IValueExpression expression) =>
        new(PathTargetValueKind.ValueExpression, expression);

    public static PathTargetValue OfContextAware(IContextAwareExpression contextAware) =>
        new(PathTargetValueKind.ContextAware, contextAware);

    public static PathTargetValue OfLiteral(object? literal) =>
        new(PathTargetValueKind.Literal, literal);

    public static PathTargetValue OfRequiredRelationship(IDefaultRelationship relationship) =>
        new(PathTargetValueKind.RequiredRelationship, relationship);

    public static PathTargetValue OfOptionalRelationship(IDefaultRelationship relationship) =>
        new(PathTargetValueKind.OptionalRelationship, relationship);

    public bool IsRelationship =>
        this.ValueKind is PathTargetValueKind.RequiredRelationship or PathTargetValueKind.OptionalRelationship;

    /// <summary>True when the value is a shared ancestor.</summary>
    public bool IsSharedRelationship => this.payload is ISharedRelationship;

    /// <summary>Land the value on template.targetField for the ancestor level being generated.</summary>
    public void ApplyTo(MasterTemplate template, PropertyInfo targetField)
    {
        _ = template.Remove(targetField);
        _ = this.ValueKind switch
        {
            PathTargetValueKind.ValueExpression => template.Put(targetField, (IValueExpression)this.payload!),
            PathTargetValueKind.ContextAware => template.Put(targetField, (IContextAwareExpression)this.payload!),
            PathTargetValueKind.Literal => template.Put(targetField, this.payload),
            PathTargetValueKind.RequiredRelationship => template.PutRequired(targetField, (IDefaultRelationship)this.payload!),
            PathTargetValueKind.OptionalRelationship => template.PutOptional(targetField, (IDefaultRelationship)this.payload!),
            _ => template,
        };
    }
}
