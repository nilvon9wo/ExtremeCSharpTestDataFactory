using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Persistence;
namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>Turns one master template plus the test's own partial templates into a wired Bundle.</summary>
public sealed class RecordFactory
{
    private readonly GenerationContext context;
    private readonly MasterTemplate template;

    private RecordFactory(GenerationContext context, MasterTemplate masterTemplate)
    {
        this.context = context;
        MasterTemplate forced = RelationshipForcer.Apply(context.ForcedRelationshipPaths, masterTemplate);
        this.template = PathValueApplier.Apply(context.PathValues, forced);
    }

    public static Bundle CreateBundle(GenerationContext context, MasterTemplate masterTemplate, List<object> testTemplates) =>
        new RecordFactory(context, masterTemplate).Build(testTemplates);

    private Bundle Build(List<object> testTemplates)
    {
        int quantity = testTemplates.Count;
        Bundle bundle = new AncestorGenerator(this.context, quantity, this.template).Generate();
        List<object> records = PlainValueFiller.CloneAndCompletePlainValues(this.template, testTemplates);
        bundle.PutPrimaries(this.template.PrimaryTargetField, records);
        new LookupWiring(bundle, this.context, this.template).Wire();
        new ContextAwareValuePass(bundle, this.context, this.template).Complete();
        this.RegisterDeferredValues(bundle);
        this.Persist(records);
        return bundle;
    }

    /// <summary>
    /// Up-flowing values are left unresolved and handed to the bundle for the
    /// DEFERRED flush to fill. In any other mode the whole forest never
    /// exists, so it is a loud error, not a silent null.
    /// </summary>
    private void RegisterDeferredValues(Bundle bundle)
    {
        if (this.template.DeferredExpressionByField.Count == 0)
        {
            return;
        }

        if (!this.context.BatchedInsertPending)
        {
            throw new XftyConfigurationException(
                "A value that reads up from a generated child needs the DEFERRED insert mode - the child must "
                + "exist before it can be read. Use InsertMode.Deferred and flush the deferred buffer.");
        }

        bundle.DeferValues(this.template.DeferredExpressionByField);
    }

    private void Persist(List<object> records) =>
        _ = this.context.InsertMode switch
        {
            InsertMode.Mock => IdMocker.AddIds(records, this.template.PrimaryTargetField),
            InsertMode.Now => this.InsertNow(records),
            _ => records,
        };

    private List<object> InsertNow(List<object> records)
    {
        if (this.context.PersistenceGateway is null)
        {
            throw new NotSupportedException(
                "InsertMode.Now needs a persistence gateway - RecordProvider.SetPersistenceGateway(...) - use "
                + "Mock or Never when none is configured.");
        }

        this.context.PersistenceGateway.Insert(records, this.template.PrimaryTargetField);
        return records;
    }
}
