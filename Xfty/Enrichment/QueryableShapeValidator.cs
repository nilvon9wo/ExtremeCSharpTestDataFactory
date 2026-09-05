using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>
/// Rejects an InjectConfig that describes a shape no single SOQL query could
/// return - a parent climb past the platform's relationship-hop limit, or a
/// child subquery nested deeper than one level. An injected shape the
/// platform could never produce is a landmine in an integration test.
///
/// config.BreakSoqlLimits() turns those checks off; past that point the
/// developer owns what they asked for. The one check it does not lift is
/// that ChildDepth reaches as deep as every InjectChildValue path - that is
/// an internal consistency error, not a SOQL-shape one.
/// </summary>
public static class QueryableShapeValidator
{
    public static void Validate(InjectConfig config)
    {
        RejectChildValuesDeeperThanChildDepth(config);
        if (config.SoqlLimitsLifted)
        {
            return;
        }

        RejectOverSoql($"parentDepth {config.ParentDepthLimit}", config.ParentDepthLimit, InjectConfig.SoqlParentHops);
        RejectOverSoql($"childDepth {config.ChildDepthLimit}", config.ChildDepthLimit, InjectConfig.SoqlChildDepth);
        config.IncludedParentPaths.ForEach(path =>
            RejectOverSoql($"an InjectParent path of {path.Count} hops", path.Count, InjectConfig.SoqlParentHops));
    }

    /// <summary>An InjectChildValue path can only place its value if the walk descends far enough - ChildDepth has to allow as many child levels as the path has.</summary>
    private static void RejectChildValuesDeeperThanChildDepth(InjectConfig config) =>
        config.ChildValues.ForEach(childValue =>
        {
            int childLevels = childValue.RelationshipPrefix().Count;
            if (childLevels > config.ChildDepthLimit)
            {
                throw new XftyConfigurationException(
                    $"Inject: an InjectChildValue path reaches {childLevels} child level(s) but childDepth is "
                    + $"{config.ChildDepthLimit}. Raise childDepth (past {InjectConfig.SoqlChildDepth} also needs BreakSoqlLimits()).");
            }
        });

    private static void RejectOverSoql(string label, int value, int soqlMax)
    {
        if (value <= soqlMax)
        {
            return;
        }

        throw new XftyConfigurationException(
            $"Inject: {label} exceeds the {soqlMax} a single SOQL query allows. Call BreakSoqlLimits() on the config to allow it.");
    }
}
