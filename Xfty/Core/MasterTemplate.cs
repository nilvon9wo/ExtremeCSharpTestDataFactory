using System.Linq.Expressions;
using System.Reflection;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The recipe for one Provider's records: default values, context-aware
/// values, deferred (up-flowing) values, and required/optional relationships,
/// keyed by field.
/// </summary>
public sealed class MasterTemplate
{
    public PropertyInfo PrimaryTargetField { get; }

    public Dictionary<PropertyInfo, IValueExpression> DefaultByField { get; }

    public Dictionary<PropertyInfo, IContextAwareExpression> ContextAwareByField { get; } = [];

    public Dictionary<PropertyInfo, IDeferredExpression> DeferredExpressionByField { get; } = [];

    public Dictionary<PropertyInfo, IDefaultRelationship> RequiredRelationshipByField { get; }

    public Dictionary<PropertyInfo, IDefaultRelationship> OptionalRelationshipByField { get; }

    // Insertion order of the value fields (plain + context-aware) - a
    // context-aware value may read an earlier one, so the value passes need a
    // deterministic order.
    private readonly List<PropertyInfo> valueFieldOrder;

    public MasterTemplate(
        PropertyInfo primaryTargetField,
        Dictionary<PropertyInfo, IValueExpression> defaultByField,
        Dictionary<PropertyInfo, IDefaultRelationship> requiredRelationshipByField,
        Dictionary<PropertyInfo, IDefaultRelationship> optionalRelationshipByField)
    {
        this.PrimaryTargetField = primaryTargetField;
        this.DefaultByField = defaultByField;
        this.RequiredRelationshipByField = requiredRelationshipByField;
        this.OptionalRelationshipByField = optionalRelationshipByField;
        this.valueFieldOrder = [.. defaultByField.Keys];
    }

    public MasterTemplate(PropertyInfo primaryTargetField)
        : this(primaryTargetField, [], [], [])
    {
    }

    public MasterTemplate Put(PropertyInfo field, IValueExpression valueTemplate)
    {
        this.TrackFieldOrder(field);
        _ = this.ContextAwareByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        this.DefaultByField[field] = valueTemplate;
        return this;
    }

    public MasterTemplate Put(PropertyInfo field, IContextAwareExpression contextAwareExpression)
    {
        this.TrackFieldOrder(field);
        _ = this.DefaultByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        this.ContextAwareByField[field] = contextAwareExpression;
        return this;
    }

    /// <summary>An up-flowing value - resolved during the DEFERRED flush.</summary>
    public MasterTemplate Put(PropertyInfo field, IDeferredExpression deferredValue)
    {
        this.TrackFieldOrder(field);
        _ = this.DefaultByField.Remove(field);
        _ = this.ContextAwareByField.Remove(field);
        this.DeferredExpressionByField[field] = deferredValue;
        return this;
    }

    public MasterTemplate PutRequired(PropertyInfo field, IDefaultRelationship relationshipTemplate)
    {
        this.RequiredRelationshipByField[field] = relationshipTemplate;
        return this;
    }

    public MasterTemplate PutOptional(PropertyInfo field, IDefaultRelationship relationshipTemplate)
    {
        this.OptionalRelationshipByField[field] = relationshipTemplate;
        return this;
    }

    /// <summary>
    /// Convenience overload, routed by runtime type: a relationship is
    /// rejected (its requiredness has to be stated via PutRequired/PutOptional);
    /// anything else is treated as an exact literal.
    /// </summary>
    public MasterTemplate Put(PropertyInfo field, object? value) =>
        value switch
        {
            IDeferredExpression deferred => this.Put(field, deferred),
            IContextAwareExpression contextAware => this.Put(field, contextAware),
            IValueExpression valueExpression => this.Put(field, valueExpression),
            IDefaultRelationship => throw new XftyConfigurationException(
                "Relationships must be added with PutRequired(...) or PutOptional(...), not Put(...)."),
            _ => this.Put(field, new LiteralExpression(value)),
        };

    // Lambda overloads - naming field by lambda instead of Field.Of<TRecord>(...) --------

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IValueExpression valueTemplate) =>
        this.Put(Field.Of(field), valueTemplate);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IContextAwareExpression contextAwareExpression) =>
        this.Put(Field.Of(field), contextAwareExpression);

    /// <summary>An up-flowing value - resolved during the DEFERRED flush. Naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IDeferredExpression deferredValue) =>
        this.Put(Field.Of(field), deferredValue);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, object? value) =>
        this.Put(Field.Of(field), value);

    /// <summary>PutRequired(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate PutRequired<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.PutRequired(Field.Of(field), relationshipTemplate);

    /// <summary>PutOptional(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate PutOptional<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.PutOptional(Field.Of(field), relationshipTemplate);

    /// <summary>Every value field (plain + context-aware) in the order it was Put.</summary>
    public List<PropertyInfo> OrderedValueFields() => [.. this.valueFieldOrder];

    /// <summary>
    /// An independent copy of this template. The field maps (and the field-
    /// order list) are recreated so a caller can add or remove entries
    /// without mutating the shared template a Provider exposes; the
    /// expression instances themselves are shared, treated as immutable
    /// configuration.
    /// </summary>
    public MasterTemplate Copy()
    {
        MasterTemplate theCopy = new(
            this.PrimaryTargetField,
            new Dictionary<PropertyInfo, IValueExpression>(this.DefaultByField),
            new Dictionary<PropertyInfo, IDefaultRelationship>(this.RequiredRelationshipByField),
            new Dictionary<PropertyInfo, IDefaultRelationship>(this.OptionalRelationshipByField));
        this.ContextAwareByField.ToList().ForEach(pair => theCopy.ContextAwareByField[pair.Key] = pair.Value);
        this.DeferredExpressionByField.ToList().ForEach(pair => theCopy.DeferredExpressionByField[pair.Key] = pair.Value);
        theCopy.valueFieldOrder.Clear();
        theCopy.valueFieldOrder.AddRange(this.valueFieldOrder);
        return theCopy;
    }

    public MasterTemplate Remove(PropertyInfo field)
    {
        _ = this.DefaultByField.Remove(field);
        _ = this.ContextAwareByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        _ = this.RequiredRelationshipByField.Remove(field);
        _ = this.OptionalRelationshipByField.Remove(field);
        _ = this.valueFieldOrder.RemoveAll(each => each == field);
        return this;
    }

    private void TrackFieldOrder(PropertyInfo field)
    {
        bool alreadyTracked = this.DefaultByField.ContainsKey(field)
            || this.ContextAwareByField.ContainsKey(field)
            || this.DeferredExpressionByField.ContainsKey(field);
        if (!alreadyTracked)
        {
            this.valueFieldOrder.Add(field);
        }
    }
}

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
public sealed class MasterTemplate<TRecord>
{
    private readonly MasterTemplate inner;

    public MasterTemplate(Expression<Func<TRecord, object?>> primaryTargetField) =>
        this.inner = new MasterTemplate(Field.Of(primaryTargetField));

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
