using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Relationships;
namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// Applies each IncludeOptional(...) path by promoting its head relationship from optional to required, on a copy of
/// the master template.
/// </summary>
public static class RelationshipForcer
{
    public static MasterTemplate Apply(List<List<PropertyInfo>> paths, MasterTemplate template)
    {
        if (paths.Count == 0)
        {
            return template;
        }

        MasterTemplate forced = template.Copy();
        paths.ForEach(path => PromoteHead(path[0], forced, template));
        return forced;
    }

    private static void PromoteHead(PropertyInfo head, MasterTemplate forced, MasterTemplate source)
    {
        if (!source.RelationshipByField.TryGetValue(head, out RelationshipConfig? config))
        {
            throw new XftyConfigurationException(
                $"IncludeOptional: {head.Name} is not a relationship on the Provider "
                + $"for {source.PrimaryTargetField.Name}."
            );
        }

        if (!config.IsRequired)
        {
            _ = forced.PutRequired(head, config.Relationship);
        }
    }
}