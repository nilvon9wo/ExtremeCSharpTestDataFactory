using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// Runs one Supply*() call from a frozen <see cref="RecordProviderPlan"/>:
/// resolve shared ancestors, build the <see cref="GenerationContext"/>, generate
/// the primary Bundle, then generate the children and persist as the insert mode
/// dictates. Separate from <see cref="RecordProvider"/> - the builder collects
/// configuration, this executes it - so the persistence-mode branching can be
/// tested against a hand-built plan.
/// </summary>
internal sealed class RecordProviderExecution(RecordProviderPlan plan)
{
    public async Task<Bundle> SupplyBundle()
    {
        this.WarnIfMixingCustomTemplateWithOverrides();
        await SharedAncestorResolver
            .ResolveAllConfigured(plan.ProviderLookup, plan.InsertMode)
            .ConfigureAwait(false);
        GenerationContext context = this.BuildContext();
        List<object> templates = this.TemplatesToFill();
        Bundle bundle = await this.Generate(context, templates).ConfigureAwait(false);
        await this.SupplyChildrenAndPersist(bundle).ConfigureAwait(false);
        return bundle;
    }

    public async Task<List<object>> SupplyList() =>
        (await this.SupplyBundle().ConfigureAwait(false)).GetList(plan.PrimaryTargetField)!;

    public async Task<object> Supply() =>
        (await this.SupplyList().ConfigureAwait(false))[0];

    private async Task SupplyChildrenAndPersist(Bundle bundle)
    {
        // Children join the same deferred graph when batched - generated
        // structurally now, FK wired when the buffer flushes. A structural child
        // of a deferred parent stays structural and persists nothing here.
        // Otherwise (Now/Mock) the primaries already have Ids after Generate();
        // the back-reference is wired concretely.
        bool batched = this.BuildsStructurallyForBatchedInsert();
        bool structural = batched || plan.ForceStructuralChildGeneration;
        await plan.ChildConfig
            .GenerateAll(bundle, structural, this.ChildExecutionState())
            .ConfigureAwait(false);
        if (batched)
        {
            await this.Persist(bundle).ConfigureAwait(false);
        }
    }

    private RecordProviderExecutionState ChildExecutionState() =>
        new(
            plan.ProviderLookup,
            plan.Outlet,
            plan.InsertMode,
            plan.Inclusivity,
            plan.PersistenceGateway);

    private Task<Bundle> Generate(GenerationContext context, List<object> templates) =>
        plan.TemplateConfig.HasCustomTemplate
            ? RecordFactory.CreateBundle(context, plan.TemplateConfig.ResolveTemplate(), templates)
            : plan.Outlet.CreateBundle(context, templates);

    private Task Persist(Bundle bundle)
    {
        if (this.FlushesGraphWhenThisCallEnds())
        {
            return DeferredInsertBuffer.InsertGraph(
                bundle,
                plan.PersistenceGateway,
                plan.ExcludePrimaryIds);
        }

        if (this.DeferredToRegistry())
        {
            DeferredInserter.Register(bundle, plan.ExcludePrimaryIds);
        }

        return Task.CompletedTask;
    }

    private GenerationContext BuildContext()
    {
        GenerationContext context =
            new GenerationContext(plan.ProviderLookup, this.ContextInsertMode(), plan.Inclusivity)
                .WithPersistenceGateway(plan.PersistenceGateway)
                .WithUnsetFieldFiller(plan.UnsetFieldFiller)
                .WithForcedRelationshipPaths(plan.TemplateConfig.ForcedRelationshipPaths)
                .WithPathValues(plan.TemplateConfig.PathValues)
                .WithAncestorCycleGuard(plan.AncestorCyclesAllowed)
                .WithPrimaryIdsExcluded(plan.ExcludePrimaryIds);
        return this.BuildsStructurallyForBatchedInsert() ? context.ForBatchedInsert() : context;
    }

    private InsertMode ContextInsertMode() =>
        this.BuildsStructurallyForBatchedInsert() ? InsertMode.Never : plan.InsertMode;

    private bool BuildsStructurallyForBatchedInsert() =>
        this.FlushesGraphWhenThisCallEnds() || this.DeferredToRegistry();

    private bool FlushesGraphWhenThisCallEnds() =>
        plan.DepthBatched && plan.InsertMode == InsertMode.Now;

    private bool DeferredToRegistry() => plan.InsertMode == InsertMode.Deferred;

    private List<object> TemplatesToFill()
    {
        List<object> templates = this.SuppliedOrBlankTemplates();
        return plan.QuantityPerTemplate > 1
            ? MultiplyByQuantity(templates, plan.QuantityPerTemplate)
            : templates;
    }

    private List<object> SuppliedOrBlankTemplates() =>
        plan.HasOverrideTemplates ? plan.OverrideTemplates! : [BlankInstances.Of(plan.RecordType)];

    private void WarnIfMixingCustomTemplateWithOverrides()
    {
        if (plan.TemplateConfig.HasCustomTemplate && plan.HasOverrideTemplates)
        {
            Console.Error.WriteLine("Custom master template + overrides: overrides win all conflicts!");
        }
    }

    private static List<object> MultiplyByQuantity(List<object> templateList, int quantity) =>
        [.. Enumerable.Range(1, quantity).SelectMany(_ => templateList)];
}