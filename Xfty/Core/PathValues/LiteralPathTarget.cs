using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Core.PathValues;

public record LiteralPathTarget(object? Literal) : IPathApplicable
{
    public bool IsRelationship()
        => false;

    public bool IsSharedRelationship()
        => this.Literal is ISharedRelatable;

    public void ApplyTo(MasterTemplate template, PropertyInfo targetField)
        => template.Put(targetField, this.Literal);
}