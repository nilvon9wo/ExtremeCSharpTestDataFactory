using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Relationships;
namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>Points each primary record's lookup at the matching generated ancestor.</summary>
public sealed class LookupWiring(Bundle bundle, GenerationContext context, MasterTemplate template)
{
    private readonly Bundle bundle = bundle;
    private readonly GenerationContext context = context;
    private readonly Dictionary<PropertyInfo, IDefaultRelationship> relationships = MergeRelationships(template);

    /// <summary>
    /// Wires whatever ancestors are actually present in the bundle. There is no
    /// need to re-check inclusivity here: <see cref="AncestorGenerator"/> already
    /// decided what belongs in the bundle, honouring both the call's inclusivity
    /// and any per-call forced relationship (<c>IncludeOptional</c>) - the latter
    /// generates fully formed even under <see cref="InsertInclusivity.None"/>, so
    /// this step must still wire it up.
    /// </summary>
    public void Wire()
    {
        List<object> records = this.bundle.PrimaryRecords()!;
        records
            .Select((record, row) => (record, row))
            .ToList()
            .ForEach(each => this.WireRecord(each.record, each.row));
    }

    private void WireRecord(object record, int row) =>
        this.relationships.Keys.ToList().ForEach(field => this.WireField(record, row, field));

    private void WireField(object record, int row, PropertyInfo field)
    {
        if (!FieldState.IsUnset(field, record))
        {
            return;
        }

        object? parent = this.ParentAt(field, row);
        if (parent is not null)
        {
            this.PointToParent(record, field, parent);
        }
    }

    private void PointToParent(object record, PropertyInfo field, object parent)
    {
        IDefaultRelationship relationship = this.relationships[field];
        PropertyInfo? parentSourceField = relationship.RelatedField ?? this.bundle.GetBundle(field)?.PrimaryTargetField;
        field.SetValue(record, parentSourceField?.GetValue(parent));
    }

    private object? ParentAt(PropertyInfo field, int row)
    {
        List<object>? parents = this.bundle.GetList(field);
        bool noParentForRow = parents is null || row >= parents.Count;
        return noParentForRow
            ? null
            : parents![row];
    }

    private static Dictionary<PropertyInfo, IDefaultRelationship> MergeRelationships(MasterTemplate template)
    {
        Dictionary<PropertyInfo, IDefaultRelationship> merged = new(template.RequiredRelationshipByField);
        template.OptionalRelationshipByField.ToList().ForEach(pair => merged[pair.Key] = pair.Value);
        return merged;
    }
}
