using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>MasterTemplate - copying, removing a field, and the value-field ordering that copy preserves.</summary>
public sealed partial class MasterTemplate
{
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
        MasterTemplate theCopy = this.CopyOwnMaps();
        CopyInto(theCopy.ContextAwareByField, this.ContextAwareByField);
        CopyInto(theCopy.DeferredExpressionByField, this.DeferredExpressionByField);
        theCopy.valueFieldOrder.Clear();
        theCopy.valueFieldOrder.AddRange(this.valueFieldOrder);
        return theCopy;
    }

    private MasterTemplate CopyOwnMaps() =>
        new(
            this.PrimaryTargetField,
            new Dictionary<PropertyInfo, IValueExpression>(this.DefaultByField),
            new Dictionary<PropertyInfo, IDefaultRelationship>(this.RequiredRelationshipByField),
            new Dictionary<PropertyInfo, IDefaultRelationship>(this.OptionalRelationshipByField));

    private static void CopyInto<TValue>(Dictionary<PropertyInfo, TValue> target, Dictionary<PropertyInfo, TValue> source) =>
        source.ToList().ForEach(pair => target[pair.Key] = pair.Value);

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
        if (!this.IsAlreadyTracked(field))
        {
            this.valueFieldOrder.Add(field);
        }
    }

    private bool IsAlreadyTracked(PropertyInfo field) =>
        this.DefaultByField.ContainsKey(field)
        || this.ContextAwareByField.ContainsKey(field)
        || this.DeferredExpressionByField.ContainsKey(field);
}
