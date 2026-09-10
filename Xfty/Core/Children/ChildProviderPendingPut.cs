using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.Children;

/// <summary>
/// One field configuration queued on a <see cref="ChildProvider"/>, applied to the real RecordProvider once it exists.
/// </summary>
public sealed class ChildProviderPendingPut
{
    private readonly PropertyInfo _field;
    private readonly ChildProviderPendingPutKind _kind;
    private readonly object? _payload;

    private ChildProviderPendingPut(PropertyInfo field, ChildProviderPendingPutKind kind, object? payload)
    {
        this._field = field;
        this._kind = kind;
        this._payload = payload;
    }

    public static ChildProviderPendingPut OfValue(PropertyInfo field, IValueExpression expression) =>
        new(field, ChildProviderPendingPutKind.Value, expression);

    public static ChildProviderPendingPut OfContextAware(PropertyInfo field, IContextAwareExpression expression) =>
        new(field, ChildProviderPendingPutKind.ContextAware, expression);

    public static ChildProviderPendingPut OfRequiredRelationship(
        PropertyInfo field,
        IDefaultRelationship relationship
    ) =>
        new(field, ChildProviderPendingPutKind.RequiredRelationship, relationship);

    public static ChildProviderPendingPut OfOptionalRelationship(
        PropertyInfo field,
        IDefaultRelationship relationship
    ) =>
        new(field, ChildProviderPendingPutKind.OptionalRelationship, relationship);

    public static ChildProviderPendingPut OfLiteral(PropertyInfo field, object? literal) =>
        new(field, ChildProviderPendingPutKind.Literal, literal);

    public void ApplyTo(RecordProvider provider) =>
        _ = this._kind switch
        {
            ChildProviderPendingPutKind.Value => provider.Put(this._field, (IValueExpression)this._payload!),
            ChildProviderPendingPutKind.ContextAware =>
                provider.Put(this._field, (IContextAwareExpression)this._payload!),
            ChildProviderPendingPutKind.RequiredRelationship =>
                provider.PutRequired(this._field, (IDefaultRelationship)this._payload!),
            ChildProviderPendingPutKind.OptionalRelationship =>
                provider.PutOptional(this._field, (IDefaultRelationship)this._payload!),
            _ => provider.Put(this._field, this._payload),
        };
}