using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;

namespace Net.NowhereAtAll.Xfty.Core.PathValues;

public interface IPathTargetValue
{
    void ApplyTo(MasterTemplate template, PropertyInfo targetField);
    bool IsRelationship();
    bool IsSharedRelationship();
}