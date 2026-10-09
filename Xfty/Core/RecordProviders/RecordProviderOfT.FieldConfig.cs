using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// RecordProvider&lt;TRecord&gt; - field and relationship configuration by
/// <see cref="PropertyInfo"/>; the <c>x =&gt; x.Field</c> forms are in
/// RecordProviderOfT.FieldConfigLambda.cs.
/// </summary>
public sealed partial class RecordProvider<TRecord>
{
    public RecordProvider<TRecord> Put(PropertyInfo field, IValueYielding valueTemplate)
    {
        _ = this._inner.Put(field, valueTemplate);
        return this;
    }

    public RecordProvider<TRecord> Put(PropertyInfo field, IContextAware contextAwareExpression)
    {
        _ = this._inner.Put(field, contextAwareExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(PropertyInfo field, IDeferred deferredValue)
    {
        _ = this._inner.Put(field, deferredValue);
        return this;
    }

    public RecordProvider<TRecord> Put(PropertyInfo field, object? value)
    {
        _ = this._inner.Put(field, value);
        return this;
    }

    public RecordProvider<TRecord> PutRequired(PropertyInfo field, IRelatable relationshipTemplate)
    {
        _ = this._inner.PutRequired(field, relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> PutOptional(PropertyInfo field, IRelatable relationshipTemplate)
    {
        _ = this._inner.PutOptional(field, relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> RemoveFromMasterTemplate(PropertyInfo field)
    {
        _ = this._inner.RemoveFromMasterTemplate(field);
        return this;
    }

    // Per-call relationship control ----------------------------------------

    public RecordProvider<TRecord> IncludeOptional(PropertyInfo field)
    {
        _ = this._inner.IncludeOptional(field);
        return this;
    }

    public RecordProvider<TRecord> IncludeOptional(List<PropertyInfo> relationshipPath)
    {
        _ = this._inner.IncludeOptional(relationshipPath);
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationship(PropertyInfo field)
    {
        _ = this._inner.ExcludeRelationship(field);
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationshipIfPresent(PropertyInfo field)
    {
        _ = this._inner.ExcludeRelationshipIfPresent(field);
        return this;
    }

    // Path-scoped value overrides -----------------------------------------

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, IValueYielding valueExpression)
    {
        _ = this._inner.Put(path, valueExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, IContextAware contextAwareExpression)
    {
        _ = this._inner.Put(path, contextAwareExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, object? literal)
    {
        _ = this._inner.Put(path, literal);
        return this;
    }

    public RecordProvider<TRecord> PutRequired(List<PropertyInfo> path, IRelatable relationship)
    {
        _ = this._inner.PutRequired(path, relationship);
        return this;
    }

    public RecordProvider<TRecord> PutOptional(List<PropertyInfo> path, IRelatable relationship)
    {
        _ = this._inner.PutOptional(path, relationship);
        return this;
    }
}