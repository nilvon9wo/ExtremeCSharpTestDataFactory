using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>RecordProvider - the Supply*() pipeline: build the context, generate, then children and persistence.</summary>
public sealed partial class RecordProvider
{
    public async Task<Bundle> SupplyBundle()
    {
        this.WarnIfMixingCustomTemplateWithOverrides();
        await SharedAncestorResolver.ResolveAllConfigured(this.providerLookup, this.insertMode);
        GenerationContext context = this.BuildContext();
        List<object> templates = this.TemplatesToFill();
        Bundle bundle = await this.Generate(context, templates);
        await this.SupplyChildrenAndPersist(bundle);
        return bundle;
    }

    private async Task SupplyChildrenAndPersist(Bundle bundle)
    {
        bool batched = this.BuildsStructurallyForBatchedInsert();
        // Children join the same deferred graph when batched - generated structurally now, FK wired when the
        // buffer flushes. A structural child of a deferred parent also stays structural, but persists nothing here.
        // Otherwise (Now/Mock), primaries already have Ids after Generate(); wire the back-reference concretely.
        await this.childConfig.GenerateAll(bundle, batched || this.forceStructuralChildGeneration, this.ExecutionState());
        if (batched)
        {
            await this.Persist(bundle);
        }
    }

    private RecordProviderExecutionState ExecutionState() =>
        new(this.providerLookup, this.ResolveFactoryOutlet(), this.insertMode, this.inclusivity, this.persistenceGateway);

    public async Task<List<object>> SupplyList() =>
        (await this.SupplyBundle()).GetList(this.ResolveFactoryOutlet().PrimaryTargetField)!;

    public async Task<object> Supply() => (await this.SupplyList())[0];

    private Task<Bundle> Generate(GenerationContext context, List<object> templates) =>
        this.templateConfig.HasCustomTemplate
            ? RecordFactory.CreateBundle(context, this.templateConfig.ResolveTemplate(), templates)
            : this.ResolveFactoryOutlet().CreateBundle(context, templates);

    private Task Persist(Bundle bundle)
    {
        if (this.FlushesGraphWhenThisCallEnds())
        {
            return DeferredInsertBuffer.InsertGraph(bundle, this.persistenceGateway, this.excludePrimaryIds);
        }

        if (this.DeferredToRegistry())
        {
            DeferredInserter.Register(bundle, this.excludePrimaryIds);
        }

        return Task.CompletedTask;
    }
}
