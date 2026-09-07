using System.Reflection;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.PathValues;

public record class ValueExpressionPathTarget(IValueExpression Expression) : IPathTargetValue
{
    public bool IsRelationship()
        => false;

    public bool IsSharedRelationship()
        => false;

    public void ApplyTo(MasterTemplate template, PropertyInfo targetField)
        => template.Put(targetField, this.Expression);

}
