using System.Reflection;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.MasterTemplates;

/// <summary>
/// The recipe for one Provider's records: default values, context-aware
/// values, deferred (up-flowing) values, and required/optional relationships,
/// keyed by field. <see cref="MasterTemplate{TRecord}"/> is the ergonomic
/// lambda-based wrapper for building one; MasterTemplate.Lambda.cs adds the
/// <c>Put&lt;TRecord&gt;(x =&gt; x.Field, ...)</c> overloads.
/// </summary>
public sealed partial class MasterTemplate(
    PropertyInfo primaryTargetField,
    Dictionary<PropertyInfo, IValueExpression> defaultByField,
    Dictionary<PropertyInfo, IDefaultRelationship> requiredRelationshipByField,
    Dictionary<PropertyInfo, IDefaultRelationship> optionalRelationshipByField)
{
    public PropertyInfo PrimaryTargetField { get; } = primaryTargetField;

    public Dictionary<PropertyInfo, IValueExpression> DefaultByField { get; } = defaultByField;

    public Dictionary<PropertyInfo, IContextAwareExpression> ContextAwareByField { get; } = [];

    public Dictionary<PropertyInfo, IDeferredExpression> DeferredExpressionByField { get; } = [];

    public Dictionary<PropertyInfo, IDefaultRelationship> RequiredRelationshipByField { get; } =
        requiredRelationshipByField;

    public Dictionary<PropertyInfo, IDefaultRelationship> OptionalRelationshipByField { get; } =
        optionalRelationshipByField;

    /// <summary>
    /// The placeholder-Id generator for this record type under
    /// <see cref="InsertMode.Mock"/>. Null - the default - means
    /// <see cref="DefaultMockIdGenerator"/>. Carried through <see cref="Copy"/>;
    /// a per-call override lands on the copy via
    /// <c>RecordProvider.SetMockIdGenerator(...)</c>.
    /// </summary>
    public IMockIdGenerator? MockIdGenerator { get; private set; }

    private readonly ValueFieldOrder _valueFieldOrder = new(defaultByField.Keys);

    public MasterTemplate(PropertyInfo primaryTargetField)
        : this(primaryTargetField, [], [], [])
    {
    }

    /// <summary>
    /// An independent copy: the field maps and the field-order list are
    /// recreated so a caller can add or remove entries without touching the
    /// shared template a Provider exposes. The expression instances themselves
    /// are shared - they are immutable configuration.
    /// </summary>
    private MasterTemplate(MasterTemplate source)
        : this(
            source.PrimaryTargetField,
            new Dictionary<PropertyInfo, IValueExpression>(source.DefaultByField),
            new Dictionary<PropertyInfo, IDefaultRelationship>(source.RequiredRelationshipByField),
            new Dictionary<PropertyInfo, IDefaultRelationship>(source.OptionalRelationshipByField))
    {
        CopyInto(this.ContextAwareByField, source.ContextAwareByField);
        CopyInto(this.DeferredExpressionByField, source.DeferredExpressionByField);
        this._valueFieldOrder = new ValueFieldOrder(source._valueFieldOrder.Snapshot());
        this.MockIdGenerator = source.MockIdGenerator;
    }

    private static void CopyInto<TValue>(
        Dictionary<PropertyInfo, TValue> target,
        Dictionary<PropertyInfo, TValue> source) =>
        source.ToList().ForEach(pair => target[pair.Key] = pair.Value);

    public MasterTemplate Copy() => new(this);

    public MasterTemplate Put(PropertyInfo field, IValueExpression valueTemplate)
    {
        this._valueFieldOrder.Append(field);
        _ = this.ContextAwareByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        this.DefaultByField[field] = valueTemplate;
        return this;
    }

    public MasterTemplate Put(PropertyInfo field, IContextAwareExpression contextAwareExpression)
    {
        this._valueFieldOrder.Append(field);
        _ = this.DefaultByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        this.ContextAwareByField[field] = contextAwareExpression;
        return this;
    }

    /// <summary>An up-flowing value - resolved during the DEFERRED flush.</summary>
    public MasterTemplate Put(PropertyInfo field, IDeferredExpression deferredValue)
    {
        this._valueFieldOrder.Append(field);
        _ = this.DefaultByField.Remove(field);
        _ = this.ContextAwareByField.Remove(field);
        this.DeferredExpressionByField[field] = deferredValue;
        return this;
    }

    /// <summary>
    /// Convenience overload, routed by the value's runtime type: a relationship
    /// is rejected (its requiredness must be stated via PutRequired/PutOptional);
    /// anything else is treated as an exact literal.
    /// </summary>
    public MasterTemplate Put(PropertyInfo field, object? value) =>
        value switch
        {
            IDeferredExpression deferred => this.Put(field, deferred),
            IContextAwareExpression contextAware => this.Put(field, contextAware),
            IValueExpression valueExpression => this.Put(field, valueExpression),
            IDefaultRelationship => throw RelationshipsNeedPutRequiredOrOptional(),
            _ => this.Put(field, new LiteralExpression(value)),
        };

    private static XftyConfigurationException RelationshipsNeedPutRequiredOrOptional() =>
        new("Relationships must be added with PutRequired(...) or PutOptional(...), not Put(...).");

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

    /// <summary>The placeholder-Id generator under <see cref="InsertMode.Mock"/> - see <see cref="MockIdGenerator"/>.</summary>
    public MasterTemplate WithMockIdGenerator(IMockIdGenerator generator)
    {
        this.MockIdGenerator = generator;
        return this;
    }

    public MasterTemplate Remove(PropertyInfo field)
    {
        _ = this.DefaultByField.Remove(field);
        _ = this.ContextAwareByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        _ = this.RequiredRelationshipByField.Remove(field);
        _ = this.OptionalRelationshipByField.Remove(field);
        this._valueFieldOrder.Remove(field);
        return this;
    }

    /// <summary>Every value field (plain + context-aware + deferred) in the order it was Put.</summary>
    public List<PropertyInfo> OrderedValueFields() => this._valueFieldOrder.Snapshot();

    /// <summary>
    /// Whether field has a default value, a context-aware value, a deferred
    /// value, a required/optional relationship, or is the primary target field
    /// itself - i.e. whether this template touches it at all. See
    /// <see cref="IUnsetFieldFiller"/>, the one consumer of the negation.
    /// </summary>
    public bool IsConfigured(PropertyInfo field) =>
        field == this.PrimaryTargetField
        || this.DefaultByField.ContainsKey(field)
        || this.ContextAwareByField.ContainsKey(field)
        || this.DeferredExpressionByField.ContainsKey(field)
        || this.RequiredRelationshipByField.ContainsKey(field)
        || this.OptionalRelationshipByField.ContainsKey(field);
}