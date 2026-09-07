using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Core.PathValues;

public interface IPathTargetValue
{
    void ApplyTo(MasterTemplate template, PropertyInfo targetField);
    bool IsRelationship();
    bool IsSharedRelationship();
}