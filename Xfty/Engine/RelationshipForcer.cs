using System.Reflection;
using Net.Nowhereatall.Xfty.Relationships;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>Applies each IncludeOptional(...) path by promoting its head relationship from optional to required, on a copy of the master template.</summary>
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
        if (!source.OptionalRelationshipByField.TryGetValue(head, out IDefaultRelationship? optional))
        {
            AssertIsRelationship(source, head);
            return;
        }

        _ = forced.Remove(head);
        _ = forced.PutRequired(head, optional);
    }

    private static void AssertIsRelationship(MasterTemplate template, PropertyInfo head)
    {
        if (template.RequiredRelationshipByField.ContainsKey(head))
        {
            return;
        }

        throw new XftyConfigurationException(
            $"IncludeOptional: {head.Name} is not a relationship on the Provider for {template.PrimaryTargetField.Name}.");
    }
}
