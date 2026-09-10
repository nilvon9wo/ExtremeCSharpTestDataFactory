using System.Reflection;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.MasterTemplates;

/// <summary>
/// The recipe for one Provider's records: value expressions - plain,
/// context-aware, or deferred (up-flowing) - and relationships (required or
/// optional), keyed by field. <see cref="MasterTemplate{TRecord}"/> is the
/// lambda-based wrapper for building one; MasterTemplate.Lambda.cs adds the
/// <c>Put&lt;TRecord&gt;(x =&gt; x.Field, ...)</c> overloads.
/// </summary>
public sealed partial class MasterTemplate(PropertyInfo primaryTargetField)
{
    public PropertyInfo PrimaryTargetField { get; } = primaryTargetField;

    /// <summary>Plain values - <see cref="IValueExpression.Get()"/> needs no context.</summary>
    internal OrderedFieldMap<IValueExpression> DefaultByField { get; } = new();

    /// <summary>Values resolved once siblings, ancestors and lookups are in place.</summary>
    internal OrderedFieldMap<IContextAwareExpression> ContextAwareByField { get; } = new();

    /// <summary>Up-flowing values resolved during the DEFERRED flush.</summary>
    internal OrderedFieldMap<IDeferredExpression> DeferredExpressionByField { get; } = new();

    /// <summary>
    /// Every relationship on this template, required or optional - see
    /// <see cref="RelationshipConfig.IsRequired"/>. One field, one entry.
    /// </summary>
    internal Dictionary<PropertyInfo, RelationshipConfig> RelationshipByField { get; } = [];

    /// <summary>
    /// The placeholder-Id generator for this record type under
    /// <see cref="InsertMode.Mock"/>. Null - the default - means
    /// <see cref="DefaultMockIdGenerator"/>. Carried through <see cref="Copy"/>;
    /// a per-call override lands on the copy via
    /// <c>RecordProvider.SetMockIdGenerator(...)</c>.
    /// </summary>
    public IMockIdGenerator? MockIdGenerator { get; private set; }

    /// <summary>
    /// An independent copy: the field maps are recreated so a caller can add or
    /// remove entries without touching the shared template a Provider exposes.
    /// The expression instances themselves are shared - they are immutable
    /// configuration.
    /// </summary>
    private MasterTemplate(MasterTemplate source)
        : this(source.PrimaryTargetField)
    {
        CopyInto(source.DefaultByField, this.DefaultByField);
        CopyInto(source.ContextAwareByField, this.ContextAwareByField);
        CopyInto(source.DeferredExpressionByField, this.DeferredExpressionByField);
        source.RelationshipByField.ToList().ForEach(pair => this.RelationshipByField[pair.Key] = pair.Value);
        this.MockIdGenerator = source.MockIdGenerator;
    }

    private static void CopyInto<TValue>(OrderedFieldMap<TValue> source, OrderedFieldMap<TValue> target) =>
        source.ToList().ForEach(pair => target.Set(pair.Key, pair.Value));

    public MasterTemplate Copy() => new(this);

    // The three value Put overloads clear the *other* value slots and the
    // relationship slot, then Set their own - which keeps the field's existing
    // order position if it is being re-Put with the same kind.

    public MasterTemplate Put(PropertyInfo field, IValueExpression valueTemplate)
    {
        _ = this.ContextAwareByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        _ = this.RelationshipByField.Remove(field);
        this.DefaultByField.Set(field, valueTemplate);
        return this;
    }

    public MasterTemplate Put(PropertyInfo field, IContextAwareExpression contextAwareExpression)
    {
        _ = this.DefaultByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        _ = this.RelationshipByField.Remove(field);
        this.ContextAwareByField.Set(field, contextAwareExpression);
        return this;
    }

    /// <summary>An up-flowing value - resolved during the DEFERRED flush.</summary>
    public MasterTemplate Put(PropertyInfo field, IDeferredExpression deferredValue)
    {
        _ = this.DefaultByField.Remove(field);
        _ = this.ContextAwareByField.Remove(field);
        _ = this.RelationshipByField.Remove(field);
        this.DeferredExpressionByField.Set(field, deferredValue);
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

    public MasterTemplate PutRequired(PropertyInfo field, IDefaultRelationship relationshipTemplate) =>
        this.PutRelationship(field, relationshipTemplate, isRequired: true);

    public MasterTemplate PutOptional(PropertyInfo field, IDefaultRelationship relationshipTemplate) =>
        this.PutRelationship(field, relationshipTemplate, isRequired: false);

    private MasterTemplate PutRelationship(PropertyInfo field, IDefaultRelationship relationship, bool isRequired)
    {
        this.ClearField(field);
        this.RelationshipByField[field] = new RelationshipConfig(relationship, isRequired);
        return this;
    }

    /// <summary>
    /// The placeholder-Id generator under <see cref="InsertMode.Mock"/> - see <see cref="MockIdGenerator"/>.
    /// </summary>
    public MasterTemplate WithMockIdGenerator(IMockIdGenerator generator)
    {
        this.MockIdGenerator = generator;
        return this;
    }

    public MasterTemplate Remove(PropertyInfo field)
    {
        this.ClearField(field);
        return this;
    }

    /// <summary>Drop every trace of field - every value map and the relationship map.</summary>
    private void ClearField(PropertyInfo field)
    {
        _ = this.DefaultByField.Remove(field);
        _ = this.ContextAwareByField.Remove(field);
        _ = this.DeferredExpressionByField.Remove(field);
        _ = this.RelationshipByField.Remove(field);
    }

    /// <summary>
    /// Whether field has a value (plain, context-aware, or deferred), a
    /// relationship, or is the primary target field itself - i.e. whether this
    /// template touches it at all. See <see cref="IUnsetFieldFiller"/>, the one
    /// consumer of the negation.
    /// </summary>
    public bool IsConfigured(PropertyInfo field) =>
        field == this.PrimaryTargetField
        || this.DefaultByField.ContainsKey(field)
        || this.ContextAwareByField.ContainsKey(field)
        || this.DeferredExpressionByField.ContainsKey(field)
        || this.RelationshipByField.ContainsKey(field);
}