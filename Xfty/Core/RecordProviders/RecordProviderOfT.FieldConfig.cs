using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// RecordProvider&lt;TRecord&gt; - field and relationship configuration by
/// <see cref="PropertyInfo"/> (the <c>x =&gt; x.Field</c> forms are in
/// RecordProviderOfT.FieldConfigLambda.cs). Mirrors <see cref="RecordProvider"/>'s
/// own FieldConfig partial - Master-Template values and relationships, the
/// per-call relationship overrides, then the path-scoped ancestor overrides -
/// and each method forwards to the identically-named <see cref="RecordProvider"/>
/// method, which carries the documentation.
/// </summary>
public sealed partial class RecordProvider<TRecord>
{
    public RecordProvider<TRecord> Put(PropertyInfo field, IValueExpression valueTemplate) =>
        this.Forwarding(() => this.inner.Put(field, valueTemplate));

    public RecordProvider<TRecord> Put(PropertyInfo field, IContextAwareExpression contextAwareExpression) =>
        this.Forwarding(() => this.inner.Put(field, contextAwareExpression));

    public RecordProvider<TRecord> Put(PropertyInfo field, IDeferredExpression deferredValue) =>
        this.Forwarding(() => this.inner.Put(field, deferredValue));

    public RecordProvider<TRecord> Put(PropertyInfo field, object? value) =>
        this.Forwarding(() => this.inner.Put(field, value));

    public RecordProvider<TRecord> PutRequired(PropertyInfo field, IDefaultRelationship relationshipTemplate) =>
        this.Forwarding(() => this.inner.PutRequired(field, relationshipTemplate));

    public RecordProvider<TRecord> PutOptional(PropertyInfo field, IDefaultRelationship relationshipTemplate) =>
        this.Forwarding(() => this.inner.PutOptional(field, relationshipTemplate));

    public RecordProvider<TRecord> RemoveFromMasterTemplate(PropertyInfo field) =>
        this.Forwarding(() => this.inner.RemoveFromMasterTemplate(field));

    // Per-call relationship control ----------------------------------------

    public RecordProvider<TRecord> IncludeOptional(PropertyInfo field) =>
        this.Forwarding(() => this.inner.IncludeOptional(field));

    public RecordProvider<TRecord> IncludeOptional(List<PropertyInfo> relationshipPath) =>
        this.Forwarding(() => this.inner.IncludeOptional(relationshipPath));

    public RecordProvider<TRecord> ExcludeRelationship(PropertyInfo field) =>
        this.Forwarding(() => this.inner.ExcludeRelationship(field));

    public RecordProvider<TRecord> ExcludeRelationshipIfPresent(PropertyInfo field) =>
        this.Forwarding(() => this.inner.ExcludeRelationshipIfPresent(field));

    // Path-scoped value overrides -----------------------------------------

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, IValueExpression valueExpression) =>
        this.Forwarding(() => this.inner.Put(path, valueExpression));

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, IContextAwareExpression contextAwareExpression) =>
        this.Forwarding(() => this.inner.Put(path, contextAwareExpression));

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, object? literal) =>
        this.Forwarding(() => this.inner.Put(path, literal));

    public RecordProvider<TRecord> PutRequired(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.Forwarding(() => this.inner.PutRequired(path, relationship));

    public RecordProvider<TRecord> PutOptional(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.Forwarding(() => this.inner.PutOptional(path, relationship));
}
