using System.Linq.Expressions;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>
/// The ergonomic, strongly-typed way to build a <see cref="ChildProvider"/> -
/// a thin wrapper, not a second implementation, matching
/// <see cref="RecordProvider{TRecord}"/>'s own pattern. Names the
/// relationship field by lambda instead of <see cref="Field.Of{TRecord}(Expression{Func{TRecord,object}})"/>,
/// and every fluent method returns this wrapper so the whole chain stays
/// typed:
///
/// <code>
/// .With(new ChildProvider&lt;Case&gt;(x => x.AccountId) { [x => x.Subject] = "Escalated" })
/// </code>
///
/// Converts implicitly to the plain <see cref="ChildProvider"/> everything
/// else (<see cref="RecordProvider.With(ChildProvider)"/> included) already
/// works with.
/// </summary>
public sealed class ChildProvider<TChild>
{
    private readonly ChildProvider inner;

    public ChildProvider(Expression<Func<TChild, object?>> relationshipField) => this.inner = new ChildProvider(Field.Of(relationshipField));

    public ChildProvider(Expression<Func<TChild, object?>> relationshipField, TChild template) =>
        this.inner = new ChildProvider(Field.Of(relationshipField), template);

    /// <summary>Object-initializer field configuration, mirroring <see cref="RecordProvider{TRecord}"/>'s own indexer.</summary>
    public object? this[Expression<Func<TChild, object?>> field]
    {
        set => _ = this.inner.Put(Field.Of(field), value);
    }

    public static implicit operator ChildProvider(ChildProvider<TChild> typed) => typed.inner;

    public PropertyInfo RelationshipField => this.inner.RelationshipField;

    public Type ChildType => this.inner.ChildType;

    public ChildProvider<TChild> SetQuantity(int quantity)
    {
        _ = this.inner.SetQuantity(quantity);
        return this;
    }

    public ChildProvider<TChild> Put(PropertyInfo field, IValueExpression valueExpression)
    {
        _ = this.inner.Put(field, valueExpression);
        return this;
    }

    public ChildProvider<TChild> Put(PropertyInfo field, IContextAwareExpression contextAwareExpression)
    {
        _ = this.inner.Put(field, contextAwareExpression);
        return this;
    }

    public ChildProvider<TChild> Put(PropertyInfo field, object? value)
    {
        _ = this.inner.Put(field, value);
        return this;
    }

    public ChildProvider<TChild> PutRequired(PropertyInfo field, IDefaultRelationship relationship)
    {
        _ = this.inner.PutRequired(field, relationship);
        return this;
    }

    public ChildProvider<TChild> PutOptional(PropertyInfo field, IDefaultRelationship relationship)
    {
        _ = this.inner.PutOptional(field, relationship);
        return this;
    }

    // Resolved to a PropertyInfo at this boundary (Field.Of(field)), exactly
    // as RecordProvider<TRecord> does and for the same reason: TChild is
    // already fixed by this wrapper's own type parameter, so a same-named
    // TField method type parameter could never be inferred from an
    // implicitly-typed lambda.

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, IValueExpression valueExpression)
    {
        _ = this.inner.Put(Field.Of(field), valueExpression);
        return this;
    }

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, IContextAwareExpression contextAwareExpression)
    {
        _ = this.inner.Put(Field.Of(field), contextAwareExpression);
        return this;
    }

    public ChildProvider<TChild> Put(Expression<Func<TChild, object?>> field, object? value)
    {
        _ = this.inner.Put(Field.Of(field), value);
        return this;
    }

    public ChildProvider<TChild> PutRequired(Expression<Func<TChild, object?>> field, IDefaultRelationship relationship)
    {
        _ = this.inner.PutRequired(Field.Of(field), relationship);
        return this;
    }

    public ChildProvider<TChild> PutOptional(Expression<Func<TChild, object?>> field, IDefaultRelationship relationship)
    {
        _ = this.inner.PutOptional(Field.Of(field), relationship);
        return this;
    }

    public ChildProvider<TChild> SetInsertMode(InsertMode insertMode)
    {
        _ = this.inner.SetInsertMode(insertMode);
        return this;
    }

    public ChildProvider<TChild> SetInclusivity(InsertInclusivity inclusivity)
    {
        _ = this.inner.SetInclusivity(inclusivity);
        return this;
    }

    public ChildProvider<TChild> WithVariant(ILookupKey variantKey)
    {
        _ = this.inner.WithVariant(variantKey);
        return this;
    }

    public ChildProvider<TChild> With(ChildProvider? grandchildProvider)
    {
        _ = this.inner.With(grandchildProvider);
        return this;
    }
}
