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
/// below it, then does one RecordInjector round-trip. Ancestors carry their
/// one-level inverse child; downward children carry their own ancestors and,
/// to childDepth, their own nested children.
///
/// Returns the enriched target records (GetList(field)) as new instances -
/// the originals are untouched. Entry: bundle.Inject(field, config) / InjectAll.
///
/// GraftAncestors / GraftChildren (BundleEnricher.Grafting.cs) are mutually
/// recursive through EnrichPosition, so they stay part of this same class -
/// splitting them into separate classes would only give each a back-reference
/// to re-enter the recursion; splitting the FILE is fine, and this one is
/// split three ways by concern (core entry/recursion here, the grafting
/// methods in BundleEnricher.Grafting.cs, the EnrichmentPosition builders in
/// BundleEnricher.Positions.cs). The parts that do not recurse are their own
/// classes: EnrichmentTarget, EnrichmentSelection, InverseAlignment,
/// ForcedValues, QueryableShapeValidator.
///
/// There is no resource-budget check here (no C# analog for a CPU-time
/// limit) - see csharp-port-idea.md.
/// </summary>
public sealed partial class BundleEnricher
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
            : throw NothingToInject(field);

    private static XftyConfigurationException NothingToInject(PropertyInfo field) =>
        new(
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

        RecordInjector injector = RecordInjector.Inject(pos.Records);
        this.GraftAncestors(injector, pos);
        this.GraftInverse(injector, pos);
        this.GraftChildren(injector, pos);
        this.ApplyForcedValues(injector, pos);
        return injector.Result();
    }

    private void GraftInverse(RecordInjector injector, EnrichmentPosition pos)
    {
        if (pos.InverseChildField is null)
        {
            return;
        }

        _ = injector.ChildRelationship(
            InjectionPathResolver.ChildRelationshipField(pos.PositionType()!, pos.InverseChildField),
            pos.InverseChildrenPerRow!);
    }
}
