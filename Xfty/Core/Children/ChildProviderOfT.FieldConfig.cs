using System.Linq.Expressions;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.Children;

/// <summary>
/// ChildProvider&lt;TChild&gt; - field and relationship configuration for the
/// generated children, by <see cref="PropertyInfo"/> or by <c>x =&gt; x.Field</c>.
/// The lambda forms resolve to a <see cref="PropertyInfo"/> at this boundary
/// (<c>Field.Of(field)</c>), exactly as
/// <see cref="RecordProviders.RecordProvider{TRecord}"/> does and for the same
/// reason: <typeparamref name="TChild"/> is already fixed by this wrapper's own
/// type parameter, so a same-named <c>TField</c> method type parameter could
/// never be inferred from an implicitly-typed lambda. Each method forwards to
/// the identically-named <see cref="ChildProvider"/> method, which carries the
/// documentation.
/// </summary>
public sealed partial class ChildProvider<TChild>
{
    public ChildProvider<TChild> Put(PropertyInfo field, IValueExpression valueExpression) =>
        this.Forwarding(() => this._inner.Put(field, valueExpression));

    public ChildProvider<TChild> Put(PropertyInfo field, IContextAwareExpression contextAwareExpression) =>
        this.Forwarding(() => this._inner.Put(field, contextAwareExpression));

    public ChildProvider<TChild> Put(PropertyInfo field, object? value) =>
        this.Forwarding(() => this._inner.Put(field, value));

    public ChildProvider<TChild> PutRequired(PropertyInfo field, IDefaultRelationship relationship) =>
        this.Forwarding(() => this._inner.PutRequired(field, relationship));

    public ChildProvider<TChild> PutOptional(PropertyInfo field, IDefaultRelationship relationship) =>
        this.Forwarding(() => this._inner.PutOptional(field, relationship));

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, IValueExpression valueExpression) =>
        this.Forwarding(() => this._inner.Put(Field.Of(field), valueExpression));

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, IContextAwareExpression contextAwareExpression) =>
        this.Forwarding(() => this._inner.Put(Field.Of(field), contextAwareExpression));

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, object? value) =>
        this.Forwarding(() => this._inner.Put(Field.Of(field), value));

    public ChildProvider<TChild> PutRequired(Expression<Func<TChild, object?>> field, IDefaultRelationship relationship) =>
        this.Forwarding(() => this._inner.PutRequired(Field.Of(field), relationship));

    public ChildProvider<TChild> PutOptional(Expression<Func<TChild, object?>> field, IDefaultRelationship relationship) =>
        this.Forwarding(() => this._inner.PutOptional(Field.Of(field), relationship));
}