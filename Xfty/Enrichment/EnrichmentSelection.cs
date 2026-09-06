using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.Enrichment;

/// <summary>
/// Answers "does this config want the ancestor / child / inverse at this
/// position?" for BundleEnricher's recursive walk. Built once from the
/// config; paths are compared as PathKey strings.
/// </summary>
public sealed class EnrichmentSelection
{
    private readonly InjectConfig config;
    private readonly HashSet<string> includedParentKeys = [];
    private readonly HashSet<string> excludedParentKeys = [];

    public EnrichmentSelection(InjectConfig config)
    {
        this.config = config;
        config.IncludedParentPaths.ForEach(this.IncludeWithPrefixes);
        config.AncestorValues.ForEach(ancestorValue => this.IncludeWithPrefixes(ancestorValue.RelationshipPrefix()));
        config.ExcludedParentPaths.ForEach(path => this.excludedParentKeys.Add(PathKey.Of(path)));
    }

    /// <summary>
    /// pathFromEntry is the upward-hop path from the entry target to this
    /// candidate ancestor - null once the walk has turned downward, so
    /// InjectParent never reaches into a child's ancestors.
    /// </summary>
    public bool WantsAncestor(List<PropertyInfo>? pathFromEntry) =>
        pathFromEntry is null
            ? this.config.FromAllParents
            : !this.HasExcludedPrefix(pathFromEntry)
                && (this.config.FromAllParents || this.includedParentKeys.Contains(PathKey.Of(pathFromEntry)));

    /// <summary>Whether an ancestor generated for the level below should carry that level back as its child subquery.</summary>
    public bool WantsInverse(PropertyInfo relationshipField) =>
        this.config.FromAllChildren && !this.config.ExcludedChildFields.Contains(relationshipField);

    /// <summary>
    /// The child relationship fields to inject at this position. childPathHere
    /// is the child-hop path already walked from the entry target (empty at
    /// the root), so InjectChild(field) is picked up at the root and an
    /// InjectChildValue path contributes its next hop wherever the walk has
    /// reached its prefix.
    /// </summary>
    public HashSet<PropertyInfo> ChildFieldsOn(Bundle subBundle, List<PropertyInfo> childPathHere)
    {
        HashSet<PropertyInfo> present = [.. subBundle.ChildRelationshipFields()];
        HashSet<PropertyInfo> wanted = this.config.FromAllChildren ? [.. present] : [];
        wanted.UnionWith(this.NamedNextHopsFollowing(childPathHere, present));
        wanted.ExceptWith(this.config.ExcludedChildFields);
        return wanted;
    }

    private HashSet<PropertyInfo> NamedNextHopsFollowing(List<PropertyInfo> childPathHere, HashSet<PropertyInfo> present)
    {
        HashSet<PropertyInfo> named = childPathHere.Count == 0
            ? [.. this.config.IncludedChildFields.Where(present.Contains)]
            : [];
        named.UnionWith(this.config.ChildValues
            .Select(childValue => NextHopAfter(childPathHere, childValue.RelationshipPrefix()))
            .Where(nextHop => nextHop is not null && present.Contains(nextHop))!);
        return named;
    }

    private static PropertyInfo? NextHopAfter(List<PropertyInfo> walked, List<PropertyInfo> fullPath) =>
        fullPath.Count > walked.Count && fullPath.Take(walked.Count).SequenceEqual(walked)
            ? fullPath[walked.Count]
            : null;

    private void IncludeWithPrefixes(List<PropertyInfo> path) =>
        Enumerable.Range(1, path.Count).ToList()
            .ForEach(length => this.includedParentKeys.Add(PathKey.Of([.. path.Take(length)])));

    private bool HasExcludedPrefix(List<PropertyInfo> path) =>
        Enumerable.Range(1, path.Count).Any(length => this.excludedParentKeys.Contains(PathKey.Of([.. path.Take(length)])));
}
