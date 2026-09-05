using System.Reflection;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The recipe for one Provider's records: default values, context-aware
/// values, deferred (up-flowing) values, and required/optional relationships,
/// keyed by field. Split by concern - this file is the field maps and the
/// three core Put(...) overloads; MasterTemplate.Lambda.cs is the
/// `Put&lt;TRecord&gt;(x => x.Field, ...)` overloads; MasterTemplate.Copy.cs is
/// Copy/Remove/field ordering. See also <see cref="MasterTemplate{TRecord}"/>,
/// the ergonomic lambda-based wrapper for building one of these.
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

    public Dictionary<PropertyInfo, IDefaultRelationship> RequiredRelationshipByField { get; } = requiredRelationshipByField;

    public Dictionary<PropertyInfo, IDefaultRelationship> OptionalRelationshipByField { get; } = optionalRelationshipByField;

    // Insertion order of the value fields (plain + context-aware) - a
    // context-aware value may read an earlier one, so the value passes need a
    // deterministic order.
    private readonly List<PropertyInfo> valueFieldOrder = [.. defaultByField.Keys];

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
}
