using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Core.PathValues;

public record RequiredRelationPathTarget(IRelatable Relationship) : IPathApplicable
{
    public bool IsRelationship()
        => true;

    public bool IsSharedRelationship()
        => this.Relationship is ISharedRelatable;

    public void ApplyTo(MasterTemplate template, PropertyInfo targetField)
        => template.PutRequired(targetField, this.Relationship);
}