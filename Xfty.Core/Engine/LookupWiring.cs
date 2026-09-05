using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Relationships;

using Net.Nowhereatall.Xfty.Core.Core;
using Net.Nowhereatall.Xfty.Core.Engine;
namespace Net.Nowhereatall.Xfty.Core.Engine;

/// <summary>Points each primary record's lookup at the matching generated ancestor.</summary>
public sealed class LookupWiring
{
    private readonly Bundle bundle;
    private readonly GenerationContext context;
    private readonly Dictionary<PropertyInfo, IDefaultRelationship> relationships;

    public LookupWiring(Bundle bundle, GenerationContext context, MasterTemplate template)
    {
        this.bundle = bundle;
        this.context = context;
        this.relationships = MergeRelationships(template);
    }

    public void Wire()
    {
        if (this.context.Inclusivity == InsertInclusivity.None)
        {
            return;
        }

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
        if (field.GetValue(record) is not null)
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
        object? value = ReadValue(parent, relationship.RelatedField);
        field.SetValue(record, value);
    }

    private object? ParentAt(PropertyInfo field, int row)
    {
        List<object>? parents = this.bundle.GetList(field);
        bool noParentForRow = parents is null || row >= parents.Count;
        return noParentForRow
            ? null
            : parents![row];
    }

    private static object? ReadValue(object parent, PropertyInfo? sourceField) =>
        sourceField is not null
            ? sourceField.GetValue(parent)
            : parent.GetType().GetProperty("Id")?.GetValue(parent);

    private static Dictionary<PropertyInfo, IDefaultRelationship> MergeRelationships(MasterTemplate template)
    {
        Dictionary<PropertyInfo, IDefaultRelationship> merged = new(template.RequiredRelationshipByField);
        template.OptionalRelationshipByField.ToList().ForEach(pair => merged[pair.Key] = pair.Value);
        return merged;
    }
}
