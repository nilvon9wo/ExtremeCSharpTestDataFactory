using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.Enrichment;

/// <summary>
/// Rejects an InjectConfig that describes a shape no single query round-trip
/// could realistically return - a parent climb past a sane relationship-hop
/// limit, or a child subquery nested deeper than one level. An injected shape
/// a real query backend could never produce is a landmine in an integration
/// test.
///
/// config.AllowDeeperGraph() turns those checks off; past that point the
/// developer owns what they asked for. The one check it does not lift is
/// that ChildDepth reaches as deep as every InjectChildValue path - that is
/// an internal consistency error, not a query-shape one.
/// </summary>
public static class QueryableShapeValidator
{
    public static void Validate(InjectConfig config)
    {
        RejectChildValuesDeeperThanChildDepth(config);
        if (config.DepthLimitsLifted)
        {
            return;
        }

        RejectOverLimit($"parentDepth {config.ParentDepthLimit}", config.ParentDepthLimit, InjectConfig.DefaultParentDepthLimit);
        RejectOverLimit($"childDepth {config.ChildDepthLimit}", config.ChildDepthLimit, InjectConfig.DefaultChildDepthLimit);
        config.IncludedParentPaths.ForEach(path =>
            RejectOverLimit($"an InjectParent path of {path.Count} hops", path.Count, InjectConfig.DefaultParentDepthLimit));
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
                    + $"{config.ChildDepthLimit}. Raise childDepth (past {InjectConfig.DefaultChildDepthLimit} also needs AllowDeeperGraph()).");
            }
        });

    private static void RejectOverLimit(string label, int value, int defaultLimit)
    {
        if (value <= defaultLimit)
        {
            return;
        }

        throw new XftyConfigurationException(
            $"Inject: {label} exceeds the {defaultLimit} a single query round-trip should reasonably return. "
            + "Call AllowDeeperGraph() on the config to allow it.");
    }
}
