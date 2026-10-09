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
    public ChildProvider<TChild> Put(PropertyInfo field, IValueYielding valueExpression)
    {
        _ = this._inner.Put(field, valueExpression);
        return this;
    }

    public ChildProvider<TChild> Put(PropertyInfo field, IContextAware contextAwareExpression)
    {
        _ = this._inner.Put(field, contextAwareExpression);
        return this;
    }

    public ChildProvider<TChild> Put(PropertyInfo field, object? value)
    {
        _ = this._inner.Put(field, value);
        return this;
    }

    public ChildProvider<TChild> PutRequired(PropertyInfo field, IRelatable relationship)
    {
        _ = this._inner.PutRequired(field, relationship);
        return this;
    }

    public ChildProvider<TChild> PutOptional(PropertyInfo field, IRelatable relationship)
    {
        _ = this._inner.PutOptional(field, relationship);
        return this;
    }

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, IValueYielding valueExpression)
    {
        _ = this._inner.Put(Field.Of(field), valueExpression);
        return this;
    }

    public ChildProvider<TChild> Put(
        Expression<Func<TChild, object?>> field,
        IContextAware contextAwareExpression
    )
    {
        _ = this._inner.Put(Field.Of(field), contextAwareExpression);
        return this;
    }

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, object? value)
    {
        _ = this._inner.Put(Field.Of(field), value);
        return this;
    }

    public ChildProvider<TChild> PutRequired(Expression<Func<TChild, object?>> field, IRelatable relationship)
    {
        _ = this._inner.PutRequired(Field.Of(field), relationship);
        return this;
    }

    public ChildProvider<TChild> PutOptional(Expression<Func<TChild, object?>> field, IRelatable relationship)
    {
        _ = this._inner.PutOptional(Field.Of(field), relationship);
        return this;
    }
}