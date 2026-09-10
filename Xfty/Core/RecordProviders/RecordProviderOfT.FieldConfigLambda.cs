using System.Linq.Expressions;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// RecordProvider&lt;TRecord&gt; - the <c>x =&gt; x.Field</c> forms of everything in
/// RecordProviderOfT.FieldConfig.cs. Resolved to a <see cref="PropertyInfo"/>
/// at this boundary (<c>Field.Of(field)</c>, not a bare forward) and dispatched
/// to the PropertyInfo overload, exactly as <see cref="MasterTemplate{TRecord}"/>
/// does: <typeparamref name="TRecord"/> is already fixed by this wrapper's own
/// type parameter, so there is no second, independently inferred generic
/// parameter for the compiler to solve from an implicitly-typed lambda (it
/// cannot - nothing else in the call fixes it), which a same-named <c>TField</c>
/// method type parameter would need. The path-scoped overrides stay
/// PropertyInfo-only, same as the non-generic.
/// </summary>
public sealed partial class RecordProvider<TRecord>
{
    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IValueExpression valueTemplate)
    {
        _ = this._inner.Put(Field.Of(field), valueTemplate);
        return this;
    }

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IContextAwareExpression contextAwareExpression)
    {
        _ = this._inner.Put(Field.Of(field), contextAwareExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IDeferredExpression deferredValue)
    {
        _ = this._inner.Put(Field.Of(field), deferredValue);
        return this;
    }

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, object? value)
    {
        _ = this._inner.Put(Field.Of(field), value);
        return this;
    }

    public RecordProvider<TRecord> PutRequired(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate)
    {
        _ = this._inner.PutRequired(Field.Of(field), relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> PutOptional(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate)
    {
        _ = this._inner.PutOptional(Field.Of(field), relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> RemoveFromMasterTemplate(Expression<Func<TRecord, object?>> field)
    {
        _ = this._inner.RemoveFromMasterTemplate(Field.Of(field));
        return this;
    }

    public RecordProvider<TRecord> IncludeOptional(Expression<Func<TRecord, object?>> field)
    {
        _ = this._inner.IncludeOptional(Field.Of(field));
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationship(Expression<Func<TRecord, object?>> field)
    {
        _ = this._inner.ExcludeRelationship(Field.Of(field));
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationshipIfPresent(Expression<Func<TRecord, object?>> field)
    {
        _ = this._inner.ExcludeRelationshipIfPresent(Field.Of(field));
        return this;
    }
}