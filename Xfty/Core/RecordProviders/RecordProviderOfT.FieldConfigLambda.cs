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
    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IValueExpression valueTemplate) =>
        this.Forwarding(() => this.inner.Put(Field.Of(field), valueTemplate));

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IContextAwareExpression contextAwareExpression) =>
        this.Forwarding(() => this.inner.Put(Field.Of(field), contextAwareExpression));

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IDeferredExpression deferredValue) =>
        this.Forwarding(() => this.inner.Put(Field.Of(field), deferredValue));

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, object? value) =>
        this.Forwarding(() => this.inner.Put(Field.Of(field), value));

    public RecordProvider<TRecord> PutRequired(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.Forwarding(() => this.inner.PutRequired(Field.Of(field), relationshipTemplate));

    public RecordProvider<TRecord> PutOptional(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.Forwarding(() => this.inner.PutOptional(Field.Of(field), relationshipTemplate));

    public RecordProvider<TRecord> RemoveFromMasterTemplate(Expression<Func<TRecord, object?>> field) =>
        this.Forwarding(() => this.inner.RemoveFromMasterTemplate(Field.Of(field)));

    public RecordProvider<TRecord> IncludeOptional(Expression<Func<TRecord, object?>> field) =>
        this.Forwarding(() => this.inner.IncludeOptional(Field.Of(field)));

    public RecordProvider<TRecord> ExcludeRelationship(Expression<Func<TRecord, object?>> field) =>
        this.Forwarding(() => this.inner.ExcludeRelationship(Field.Of(field)));

    public RecordProvider<TRecord> ExcludeRelationshipIfPresent(Expression<Func<TRecord, object?>> field) =>
        this.Forwarding(() => this.inner.ExcludeRelationshipIfPresent(Field.Of(field)));
}
