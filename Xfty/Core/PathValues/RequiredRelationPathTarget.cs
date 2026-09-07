using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Core.PathValues;

public record class RequiredRelationPathTarget(IDefaultRelationship Relationship) : IPathTargetValue
{
    public bool IsRelationship()
        => true;

    public bool IsSharedRelationship()
        => this.Relationship is ISharedRelationship;

    public void ApplyTo(MasterTemplate template, PropertyInfo targetField)
        => template.PutRequired(targetField, this.Relationship);
}