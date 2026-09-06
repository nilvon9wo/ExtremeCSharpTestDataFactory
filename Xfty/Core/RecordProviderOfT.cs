using System.Linq.Expressions;
using System.Reflection;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The ergonomic, strongly-typed way to start and run a <see cref="RecordProvider"/>
/// for one record type - a thin wrapper, not a second implementation,
/// mirroring <see cref="MasterTemplate{TRecord}"/>'s own pattern. Every
/// fluent method forwards to the wrapped plain <see cref="RecordProvider"/>
/// and returns this wrapper, so the whole chain - including
/// <see cref="Supply"/>/<see cref="SupplyList"/>'s return type - stays
/// typed as <typeparamref name="TRecord"/> instead of <c>object</c>, with
/// no cast at the call site:
///
/// <code>
/// Contact result = new RecordProvider&lt;Contact&gt;(lookup)
///     .Put(x => x.FirstName, "Alice")
///     .SetInsertMode(InsertMode.Mock)
///     .Supply();
/// </code>
///
/// Converts implicitly to the plain <see cref="RecordProvider"/> for
/// anything not exposed here directly. <see cref="RecordProvider"/> is
/// sealed, so this is composition (an inner instance), not inheritance -
/// same reason <see cref="MasterTemplate{TRecord}"/> wraps rather than
/// extends <see cref="MasterTemplate"/>.
/// </summary>
public sealed class RecordProvider<TRecord>(IProviderLookup providerLookup)
{
    private readonly RecordProvider inner = new(typeof(TRecord), providerLookup);

    /// <summary>
    /// Object-initializer field configuration, mirroring <see cref="MasterTemplate{TRecord}"/>'s
    /// own indexer: routed by the value's runtime type (an <see cref="IValueExpression"/>,
    /// an <see cref="IContextAwareExpression"/>, an <see cref="IDeferredExpression"/>, or
    /// an exact literal). A relationship throws, naming <see cref="PutRequired{TField}"/>/
    /// <see cref="PutOptional{TField}"/> instead - its requiredness can't be inferred from the
    /// value alone.
    /// </summary>
    public object? this[Expression<Func<TRecord, object?>> field]
    {
        set => _ = this.inner.Put(Field.Of(field), value);
    }

    public static implicit operator RecordProvider(RecordProvider<TRecord> typed) => typed.inner;

    // Terminal (Supply) ---------------------------------------------------

    public TRecord Supply() => (TRecord)this.inner.Supply();

    public List<TRecord> SupplyList() => [.. this.inner.SupplyList().Cast<TRecord>()];

    public Bundle SupplyBundle() => this.inner.SupplyBundle();

    // Child collections -----------------------------------------------------

    public RecordProvider<TRecord> With(ChildProvider childProvider)
    {
        _ = this.inner.With(childProvider);
        return this;
    }

    public RecordProvider<TRecord> WithChildren(PropertyInfo childRelationshipField, int countPerParent)
    {
        _ = this.inner.WithChildren(childRelationshipField, countPerParent);
        return this;
    }

    public RecordProvider<TRecord> WithChild(PropertyInfo childRelationshipField)
    {
        _ = this.inner.WithChild(childRelationshipField);
        return this;
    }

    // Field / relationship configuration - PropertyInfo-based ------------

    public RecordProvider<TRecord> Put(PropertyInfo field, IValueExpression valueTemplate)
    {
        _ = this.inner.Put(field, valueTemplate);
        return this;
    }

    public RecordProvider<TRecord> Put(PropertyInfo field, IContextAwareExpression contextAwareExpression)
    {
        _ = this.inner.Put(field, contextAwareExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(PropertyInfo field, IDeferredExpression deferredValue)
    {
        _ = this.inner.Put(field, deferredValue);
        return this;
    }

    public RecordProvider<TRecord> Put(PropertyInfo field, object? value)
    {
        _ = this.inner.Put(field, value);
        return this;
    }

    public RecordProvider<TRecord> PutRequired(PropertyInfo field, IDefaultRelationship relationshipTemplate)
    {
        _ = this.inner.PutRequired(field, relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> PutOptional(PropertyInfo field, IDefaultRelationship relationshipTemplate)
    {
        _ = this.inner.PutOptional(field, relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> RemoveFromMasterTemplate(PropertyInfo field)
    {
        _ = this.inner.RemoveFromMasterTemplate(field);
        return this;
    }

    public RecordProvider<TRecord> IncludeOptional(PropertyInfo field)
    {
        _ = this.inner.IncludeOptional(field);
        return this;
    }

    public RecordProvider<TRecord> IncludeOptional(List<PropertyInfo> relationshipPath)
    {
        _ = this.inner.IncludeOptional(relationshipPath);
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationship(PropertyInfo field)
    {
        _ = this.inner.ExcludeRelationship(field);
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationshipIfPresent(PropertyInfo field)
    {
        _ = this.inner.ExcludeRelationshipIfPresent(field);
        return this;
    }

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, IValueExpression valueExpression)
    {
        _ = this.inner.Put(path, valueExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, IContextAwareExpression contextAwareExpression)
    {
        _ = this.inner.Put(path, contextAwareExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(List<PropertyInfo> path, object? literal)
    {
        _ = this.inner.Put(path, literal);
        return this;
    }

    public RecordProvider<TRecord> PutRequired(List<PropertyInfo> path, IDefaultRelationship relationship)
    {
        _ = this.inner.PutRequired(path, relationship);
        return this;
    }

    public RecordProvider<TRecord> PutOptional(List<PropertyInfo> path, IDefaultRelationship relationship)
    {
        _ = this.inner.PutOptional(path, relationship);
        return this;
    }

    // Field / relationship configuration - lambda-based -------------------
    //
    // Resolved to a PropertyInfo at this boundary (Field.Of(field), not a
    // bare forward) and dispatched to the PropertyInfo-based inner overload,
    // exactly as MasterTemplate<TRecord> does: TRecord is already fixed by
    // this wrapper's own type parameter, so there is no second, independently
    // inferred generic parameter for the compiler to solve for from an
    // implicitly-typed lambda (it cannot - nothing else in the call fixes
    // it), which a same-named TField method type parameter would need.

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IValueExpression valueTemplate)
    {
        _ = this.inner.Put(Field.Of(field), valueTemplate);
        return this;
    }

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IContextAwareExpression contextAwareExpression)
    {
        _ = this.inner.Put(Field.Of(field), contextAwareExpression);
        return this;
    }

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, IDeferredExpression deferredValue)
    {
        _ = this.inner.Put(Field.Of(field), deferredValue);
        return this;
    }

    public RecordProvider<TRecord> Put(Expression<Func<TRecord, object?>> field, object? value)
    {
        _ = this.inner.Put(Field.Of(field), value);
        return this;
    }

    public RecordProvider<TRecord> PutRequired(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate)
    {
        _ = this.inner.PutRequired(Field.Of(field), relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> PutOptional(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate)
    {
        _ = this.inner.PutOptional(Field.Of(field), relationshipTemplate);
        return this;
    }

    public RecordProvider<TRecord> RemoveFromMasterTemplate(Expression<Func<TRecord, object?>> field)
    {
        _ = this.inner.RemoveFromMasterTemplate(Field.Of(field));
        return this;
    }

    public RecordProvider<TRecord> IncludeOptional(Expression<Func<TRecord, object?>> field)
    {
        _ = this.inner.IncludeOptional(Field.Of(field));
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationship(Expression<Func<TRecord, object?>> field)
    {
        _ = this.inner.ExcludeRelationship(Field.Of(field));
        return this;
    }

    public RecordProvider<TRecord> ExcludeRelationshipIfPresent(Expression<Func<TRecord, object?>> field)
    {
        _ = this.inner.ExcludeRelationshipIfPresent(Field.Of(field));
        return this;
    }

    // Setters ---------------------------------------------------------------

    public RecordProvider<TRecord> SetQuantityPerTemplate(int quantityPerListedTemplate)
    {
        _ = this.inner.SetQuantityPerTemplate(quantityPerListedTemplate);
        return this;
    }

    public RecordProvider<TRecord> SetOverrideTemplateList(List<object> overrideTemplateList)
    {
        _ = this.inner.SetOverrideTemplateList(overrideTemplateList);
        return this;
    }

    public RecordProvider<TRecord> SetOverrideTemplate(object overrideTemplate)
    {
        _ = this.inner.SetOverrideTemplate(overrideTemplate);
        return this;
    }

    public RecordProvider<TRecord> WithVariant(ILookupKey variantKey)
    {
        _ = this.inner.WithVariant(variantKey);
        return this;
    }

    public RecordProvider<TRecord> SetInsertMode(InsertMode insertMode)
    {
        _ = this.inner.SetInsertMode(insertMode);
        return this;
    }

    public RecordProvider<TRecord> SetInclusivity(InsertInclusivity inclusivity)
    {
        _ = this.inner.SetInclusivity(inclusivity);
        return this;
    }

    public RecordProvider<TRecord> SetPersistenceGateway(IPersistenceGateway gateway)
    {
        _ = this.inner.SetPersistenceGateway(gateway);
        return this;
    }

    public RecordProvider<TRecord> SetUnsetFieldFiller(IUnsetFieldFiller filler)
    {
        _ = this.inner.SetUnsetFieldFiller(filler);
        return this;
    }

    public RecordProvider<TRecord> AllowAncestorCycles()
    {
        _ = this.inner.AllowAncestorCycles();
        return this;
    }

    public RecordProvider<TRecord> ExcludePrimaryIds()
    {
        _ = this.inner.ExcludePrimaryIds();
        return this;
    }

    public RecordProvider<TRecord> IncludePrimaryIds()
    {
        _ = this.inner.IncludePrimaryIds();
        return this;
    }

    public RecordProvider<TRecord> DepthBatched()
    {
        _ = this.inner.DepthBatched();
        return this;
    }

    public RecordProvider<TRecord> ForceStructuralChildGeneration()
    {
        _ = this.inner.ForceStructuralChildGeneration();
        return this;
    }
}
