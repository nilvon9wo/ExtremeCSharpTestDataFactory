using Net.Nowhereatall.Xfty.Core.Core;
using Net.Nowhereatall.Xfty.Core.Engine;
namespace Net.Nowhereatall.Xfty.Core.Engine;

/// <summary>
/// Applies the Put(List&lt;PropertyInfo&gt; path, value) overrides that have
/// reached their target - IsAtTarget() - by landing each value on a copy of
/// the master template for the level being generated. The relationship walk
/// that got here is handled by <see cref="RelationshipForcer"/>.
/// </summary>
public static class PathValueApplier
{
    public static MasterTemplate Apply(List<PathValue> pathValues, MasterTemplate template)
    {
        List<PathValue> atTarget = pathValues.Where(pathValue => pathValue.IsAtTarget()).ToList();
        if (atTarget.Count == 0)
        {
            return template;
        }

        MasterTemplate overlaid = template.Copy();
        atTarget.ForEach(pathValue => pathValue.ApplyTo(overlaid));
        return overlaid;
    }
}
