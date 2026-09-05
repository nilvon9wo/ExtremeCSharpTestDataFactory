using System.Reflection;
using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>BundleEnricher - building the EnrichmentPosition for the root, an ancestor hop, or a child collection.</summary>
public sealed partial class BundleEnricher
{
    private EnrichmentPosition RootPosition(EnrichmentTarget target)
    {
        EnrichmentPosition root = new(target.SubBundle, target.Records)
        {
            PathFromEntry = [],
            ChildPathFromEntry = [],
            ParentDepthLeft = this.config.ParentDepthLimit,
            ChildDepthLeft = this.config.ChildDepthLimit,
            IsRoot = true,
        };
        if (target.IsGeneratedAncestor)
        {
            root.CarryInverse(
                this.entryField,
                InverseAlignment.ChildrenPerParent(target.Records!, this.entryBundle.PrimaryRecords()!, this.entryField));
        }

        return root;
    }

    private EnrichmentPosition AncestorPosition(EnrichmentPosition pos, PropertyInfo lookupField, List<object> parents)
    {
        EnrichmentPosition up = new(pos.SubBundle!.GetBundle(lookupField), parents)
        {
            PathFromEntry = AncestorPath(pos, lookupField),
            ParentDepthLeft = pos.ParentDepthLeft - 1,
        };
        if (this.selection.WantsInverse(lookupField))
        {
            up.CarryInverse(lookupField, InverseAlignment.ChildrenPerParent(parents, pos.Records!, lookupField));
        }

        return up;
    }

    private EnrichmentPosition ChildrenPosition(EnrichmentPosition pos, BundleChildEntry entry, PropertyInfo childField) =>
        new(entry.Bundle, entry.Bundle.PrimaryRecords())
        {
            ParentDepthLeft = this.config.ParentDepthLimit,
            ChildDepthLeft = pos.ChildDepthLeft - 1,
            ChildPathFromEntry = Append(ChildPathOf(pos), childField),
        };

    private static List<PropertyInfo> ChildPathOf(EnrichmentPosition pos) => pos.ChildPathFromEntry ?? [];

    private static List<PropertyInfo>? AncestorPath(EnrichmentPosition pos, PropertyInfo lookupField) =>
        pos.PathFromEntry is null
            ? null
            : Append(pos.PathFromEntry, lookupField);

    private static List<PropertyInfo> Append(List<PropertyInfo> path, PropertyInfo extra) => [.. path, extra];

    private static List<List<object>> EmptyListsFor(int size) => [.. Enumerable.Range(0, size).Select(_ => new List<object>())];
}
