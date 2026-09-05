using System.Reflection;
using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>BundleEnricher - grafting ancestors, the inverse child, downward children, and forced values onto one position's records.</summary>
public sealed partial class BundleEnricher
{
    private void ApplyForcedValues(RecordInjector injector, EnrichmentPosition pos)
    {
        int rowCount = pos.Records!.Count;
        if (pos.IsRoot)
        {
            this.forcedValues.ApplyRecordValues(injector, rowCount);
        }

        if (pos.PathFromEntry is { Count: > 0 })
        {
            this.forcedValues.ApplyAncestorValues(injector, pos.PathFromEntry, rowCount);
        }

        if (pos.ChildPathFromEntry is { Count: > 0 })
        {
            this.forcedValues.ApplyChildValues(injector, pos.ChildPathFromEntry, rowCount);
        }
    }

    private void GraftAncestors(RecordInjector injector, EnrichmentPosition pos)
    {
        if (pos.SubBundle is null || pos.ParentDepthLeft <= 0)
        {
            return;
        }

        pos.SubBundle.RelationshipFields().ToList().ForEach(lookupField => this.GraftAncestor(injector, pos, lookupField));
    }

    private void GraftAncestor(RecordInjector injector, EnrichmentPosition pos, PropertyInfo lookupField)
    {
        if (!this.selection.WantsAncestor(AncestorPath(pos, lookupField)))
        {
            return;
        }

        List<object>? parents = pos.SubBundle!.GetList(lookupField);
        if (parents is null)
        {
            return;
        }

        _ = injector.Relationship(
            InjectionPathResolver.ParentRelationshipField(lookupField),
            this.EnrichPosition(this.AncestorPosition(pos, lookupField, parents)));
    }

    private void GraftChildren(RecordInjector injector, EnrichmentPosition pos)
    {
        if (pos.SubBundle is null || pos.ChildDepthLeft <= 0)
        {
            return;
        }

        this.selection.ChildFieldsOn(pos.SubBundle, ChildPathOf(pos)).ToList().ForEach(childField => this.GraftOneChildField(injector, pos, childField));
    }

    private void GraftOneChildField(RecordInjector injector, EnrichmentPosition pos, PropertyInfo childField) =>
        injector.ChildRelationship(
            InjectionPathResolver.ChildRelationshipField(pos.PositionType()!, childField),
            this.ChildrenPerRow(pos, childField));

    private List<List<object>> ChildrenPerRow(EnrichmentPosition pos, PropertyInfo childField)
    {
        List<List<object>> perRow = EmptyListsFor(pos.Records!.Count);
        pos.SubBundle!.ChildEntries(childField)
            .ForEach(entry => this.EnrichEntryInto(perRow, this.ChildrenPosition(pos, entry, childField), entry));
        return perRow;
    }

    private void EnrichEntryInto(List<List<object>> perRow, EnrichmentPosition childPos, BundleChildEntry entry)
    {
        if (childPos.Records is not { Count: > 0 })
        {
            return;
        }

        this.EnrichPosition(childPos)
            .Select((child, childRow) => (child, childRow))
            .ToList()
            .ForEach(pair => perRow[entry.ParentRowByChildRow[pair.childRow]].Add(pair.child));
    }
}
