using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Relationships;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Core;

/// <summary>One field configuration queued on a <see cref="ChildProvider"/>, applied to the real RecordProvider once it exists.</summary>
public sealed class ChildProviderPendingPut
{
    private readonly PropertyInfo field;
    private readonly ChildProviderPendingPutKind kind;
    private readonly object? payload;

    private ChildProviderPendingPut(PropertyInfo field, ChildProviderPendingPutKind kind, object? payload)
    {
        this.field = field;
        this.kind = kind;
        this.payload = payload;
    }

    public static ChildProviderPendingPut OfValue(PropertyInfo field, IValueExpression expression) =>
        new(field, ChildProviderPendingPutKind.Value, expression);

    public static ChildProviderPendingPut OfContextAware(PropertyInfo field, IContextAwareExpression expression) =>
        new(field, ChildProviderPendingPutKind.ContextAware, expression);

    public static ChildProviderPendingPut OfRequiredRelationship(PropertyInfo field, IDefaultRelationship relationship) =>
        new(field, ChildProviderPendingPutKind.RequiredRelationship, relationship);

    public static ChildProviderPendingPut OfOptionalRelationship(PropertyInfo field, IDefaultRelationship relationship) =>
        new(field, ChildProviderPendingPutKind.OptionalRelationship, relationship);

    public static ChildProviderPendingPut OfLiteral(PropertyInfo field, object? literal) =>
        new(field, ChildProviderPendingPutKind.Literal, literal);

    public void ApplyTo(RecordProvider provider) =>
        _ = this.kind switch
        {
            ChildProviderPendingPutKind.Value => provider.Put(this.field, (IValueExpression)this.payload!),
            ChildProviderPendingPutKind.ContextAware => provider.Put(this.field, (IContextAwareExpression)this.payload!),
            ChildProviderPendingPutKind.RequiredRelationship => provider.PutRequired(this.field, (IDefaultRelationship)this.payload!),
            ChildProviderPendingPutKind.OptionalRelationship => provider.PutOptional(this.field, (IDefaultRelationship)this.payload!),
            _ => provider.Put(this.field, this.payload),
        };
}
