using System.Reflection;
using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>
/// Re-expresses a generated graph in the shape an init-only property rejects,
/// so code under test reads relationships and generated child collections
/// straight off the record.
///
/// EnrichPosition walks the graph deepest-first via the call stack: at each
/// bundle position it collects the enriched parents and child subqueries
/// below it, then does one SObjectInjector round-trip. Ancestors carry their
/// one-level inverse child; downward children carry their own ancestors and,
/// to childDepth, their own nested children.
///
/// Returns the enriched target records (GetList(field)) as new instances -
/// the originals are untouched. Entry: bundle.Inject(field, config) / InjectAll.
///
/// GraftAncestors / GraftChildren are mutually recursive through
/// EnrichPosition, so they stay in one class - splitting them would only give
/// each a back-reference to re-enter the recursion. The parts that do not
/// recurse are their own classes: EnrichmentTarget, EnrichmentSelection,
/// InverseAlignment, ForcedValues, QueryableShapeValidator.
///
/// XFTY_GovernorBudget is not ported (no C# analog for Limits.getCpuTime()) -
/// see csharp-port-idea.md.
/// </summary>
public sealed class BundleEnricher
{
    private readonly Bundle entryBundle;
    private readonly PropertyInfo entryField;
    private readonly InjectConfig config;
    private readonly EnrichmentSelection selection;
    private readonly ForcedValues forcedValues;

    private BundleEnricher(Bundle bundle, PropertyInfo field, InjectConfig config)
    {
        QueryableShapeValidator.Validate(config);
        this.entryBundle = bundle;
        this.entryField = field;
        this.config = config;
        this.selection = new EnrichmentSelection(config);
        this.forcedValues = new ForcedValues(config);
    }

    public static List<object> Enrich(Bundle bundle, PropertyInfo field, InjectConfig config) =>
        new BundleEnricher(bundle, field, config).Run();

    /// <summary>Enrich with InjectConfig.Everything(); throws when the graph has nothing to inject.</summary>
    public static List<object> EnrichEverything(Bundle bundle, PropertyInfo field) =>
        EnrichmentTarget.Locate(bundle, field).HasAnythingToInject()
            ? Enrich(bundle, field, InjectConfig.Everything())
            : throw new XftyConfigurationException(
                $"InjectAll({field.Name}): the graph has no generated ancestor or child collection to inject. "
                + "Generate related records, or use Inject(field, config) with explicit values.");

    private List<object> Run()
    {
        EnrichmentTarget target = EnrichmentTarget.Locate(this.entryBundle, this.entryField);
        List<object> result = this.EnrichPosition(this.RootPosition(target));
        this.forcedValues.AssertEveryPathWasReached();
        return result;
    }

    private List<object> EnrichPosition(EnrichmentPosition pos)
    {
        if (pos.Records is not { Count: > 0 })
        {
            return pos.Records ?? [];
        }

        SObjectInjector injector = SObjectInjector.Inject(pos.Records);
        this.GraftAncestors(injector, pos);
        this.GraftInverse(injector, pos);
        this.GraftChildren(injector, pos);
        this.ApplyForcedValues(injector, pos);
        return injector.Result();
    }

    private void ApplyForcedValues(SObjectInjector injector, EnrichmentPosition pos)
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

    private void GraftAncestors(SObjectInjector injector, EnrichmentPosition pos)
    {
        if (pos.SubBundle is null || pos.ParentDepthLeft <= 0)
        {
            return;
        }

        pos.SubBundle.RelationshipFields().ToList().ForEach(lookupField => this.GraftAncestor(injector, pos, lookupField));
    }

    private void GraftAncestor(SObjectInjector injector, EnrichmentPosition pos, PropertyInfo lookupField)
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

    private void GraftInverse(SObjectInjector injector, EnrichmentPosition pos)
    {
        if (pos.InverseChildField is null)
        {
            return;
        }

        _ = injector.ChildRelationship(
            InjectionPathResolver.ChildRelationshipField(pos.PositionType()!, pos.InverseChildField),
            pos.InverseChildrenPerRow!);
    }

    private void GraftChildren(SObjectInjector injector, EnrichmentPosition pos)
    {
        if (pos.SubBundle is null || pos.ChildDepthLeft <= 0)
        {
            return;
        }

        this.selection.ChildFieldsOn(pos.SubBundle, ChildPathOf(pos)).ToList().ForEach(childField =>
            injector.ChildRelationship(
                InjectionPathResolver.ChildRelationshipField(pos.PositionType()!, childField),
                this.ChildrenPerRow(pos, childField)));
    }

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
