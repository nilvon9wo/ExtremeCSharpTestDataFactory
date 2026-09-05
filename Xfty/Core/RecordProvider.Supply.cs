using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>RecordProvider - the Supply*() pipeline: build the context, generate, then children and persistence.</summary>
public sealed partial class RecordProvider
{
    public Bundle SupplyBundle()
    {
        this.WarnIfMixingCustomTemplateWithOverrides();
        SharedAncestorResolver.ResolveAllConfigured(this.providerLookup, this.insertMode);
        GenerationContext context = this.BuildContext();
        List<object> templates = this.TemplatesToFill();
        Bundle bundle = this.Generate(context, templates);
        this.SupplyChildrenAndPersist(bundle);
        return bundle;
    }

    private void SupplyChildrenAndPersist(Bundle bundle)
    {
        bool batched = this.BuildsStructurallyForBatchedInsert();
        // Children join the same deferred graph when batched - generated structurally now, FK wired when the
        // buffer flushes. A structural child of a deferred parent also stays structural, but persists nothing here.
        // Otherwise (Now/Mock), primaries already have Ids after Generate(); wire the back-reference concretely.
        this.childConfig.GenerateAll(bundle, batched || this.forceStructuralChildGeneration, this.ExecutionState());
        if (batched)
        {
            this.Persist(bundle);
        }
    }

    private RecordProviderExecutionState ExecutionState() =>
        new(this.providerLookup, this.ResolveFactoryOutlet(), this.insertMode, this.inclusivity, this.persistenceGateway);

    public List<object> SupplyList() => this.SupplyBundle().GetList(this.ResolveFactoryOutlet().PrimaryTargetField)!;

    public object Supply() => this.SupplyList()[0];

    private Bundle Generate(GenerationContext context, List<object> templates) =>
        this.templateConfig.HasCustomTemplate
            ? RecordFactory.CreateBundle(context, this.templateConfig.ResolveTemplate(), templates)
            : this.ResolveFactoryOutlet().CreateBundle(context, templates);

    private void Persist(Bundle bundle)
    {
        if (this.FlushesGraphWhenThisCallEnds())
        {
            DeferredInsertBuffer.InsertGraph(bundle, this.persistenceGateway);
        }
        else if (this.DeferredToRegistry())
        {
            DeferredInserter.Register(bundle);
        }
    }
}
