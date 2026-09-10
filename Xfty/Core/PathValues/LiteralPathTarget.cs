using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Core.PathValues;

public record class LiteralPathTarget(object? Literal) : IPathTargetValue
{
    public bool IsRelationship()
        => false;

    public bool IsSharedRelationship()
        => this.Literal is ISharedRelationship;

    public void ApplyTo(MasterTemplate template, PropertyInfo targetField)
        => template.Put(targetField, this.Literal);
}