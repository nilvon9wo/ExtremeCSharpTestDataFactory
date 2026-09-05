using System.Linq.Expressions;
using Net.Nowhereatall.Xfty.Relationships;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The ergonomic, strongly-typed way to build a <see cref="MasterTemplate"/> for
/// one record type - a thin lambda-based wrapper, not a second implementation.
/// The indexer accepts an object-initializer entry per field, routed by the
/// value's runtime type exactly like <see cref="MasterTemplate.Put(PropertyInfo,object)"/>:
///
/// <code>
/// private static readonly MasterTemplate Template = new MasterTemplate&lt;Account&gt;(x => x.Id)
/// {
///     [x => x.Name] = new IncrementingStringExpression(DefaultNamePrefix),
///     [x => x.Industry] = new LiteralExpression(DefaultIndustry),
/// };
/// </code>
///
/// Converts implicitly to the plain <see cref="MasterTemplate"/> every other
/// class in this library works with, so it only needs to exist at the point a
/// Provider author writes one.
/// </summary>
public sealed class MasterTemplate<TRecord>(Expression<Func<TRecord, object?>> primaryTargetField)
{
    private readonly MasterTemplate inner = new MasterTemplate(Field.Of(primaryTargetField));

    public object? this[Expression<Func<TRecord, object?>> field]
    {
        set => this.inner.Put(Field.Of(field), value);
    }

    /// <summary>
    /// Chainable alternative to the indexer, for building up a template across
    /// several statements instead of one object initializer.
    /// </summary>
    public MasterTemplate<TRecord> Put(Expression<Func<TRecord, object?>> field, object? value)
    {
        _ = this.inner.Put(Field.Of(field), value);
        return this;
    }

    public MasterTemplate<TRecord> PutRequired(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationship)
    {
        _ = this.inner.PutRequired(Field.Of(field), relationship);
        return this;
    }

    public MasterTemplate<TRecord> PutOptional(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationship)
    {
        _ = this.inner.PutOptional(Field.Of(field), relationship);
        return this;
    }

    public static implicit operator MasterTemplate(MasterTemplate<TRecord> typed) => typed.inner;
}
