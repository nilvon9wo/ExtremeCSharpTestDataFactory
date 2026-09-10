using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Persistence;
namespace Net.NowhereAtAll.Xfty.Engine;

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

    public static Task<Bundle> CreateBundle(GenerationContext context, MasterTemplate masterTemplate, List<object> testTemplates) =>
        new RecordFactory(context, masterTemplate).Build(testTemplates);

    private async Task<Bundle> Build(List<object> testTemplates)
    {
        int quantity = testTemplates.Count;
        Bundle bundle = await new AncestorGenerator(this.context, quantity, this.template).Generate().ConfigureAwait(false);
        List<object> records = PlainValueFiller.CloneAndCompletePlainValues(this.template, testTemplates);
        bundle.PutPrimaries(this.template.PrimaryTargetField, records, this.template.MockIdGenerator);
        new LookupWiring(bundle, this.context, this.template).Wire();
        new ContextAwareValuePass(bundle, this.context, this.template).Complete();
        this.RegisterDeferredValues(bundle);
        this.FillUnsetFields(records);
        await this.Persist(records).ConfigureAwait(false);
        return bundle;
    }

    /// <summary>
    /// Runs after every value/relationship pass, before Persist(...) - late
    /// enough that a filler never fights XFTY for a field XFTY actually set
    /// (see <see cref="MasterTemplate.IsConfigured"/>), early enough that a
    /// real InsertMode.Now database still sees a value for a NOT NULL column
    /// XFTY itself never cared about.
    /// </summary>
    private void FillUnsetFields(List<object> records)
    {
        if (this.context.UnsetFieldFiller is not { } filler)
        {
            return;
        }

        List<PropertyInfo> unsetFields = [.. this.template.PrimaryTargetField.DeclaringType!
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.CanWrite && field.GetIndexParameters().Length == 0 && !this.template.IsConfigured(field))];
        if (unsetFields.Count > 0)
        {
            records.ForEach(record => filler.Fill(record, unsetFields));
        }
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

    /// <summary>
    /// ExcludePrimaryIds always wins - it means "this call's own primary,"
    /// and this method only ever runs for the record(s) whichever call
    /// (top-level or ForRelated's own recursion) actually owns; an ancestor
    /// never carries the flag (see GenerationContext.ForRelated).
    /// </summary>
    private Task Persist(List<object> records) =>
        this.context.ExcludePrimaryIds
            ? Task.CompletedTask
            : this.context.InsertMode switch
            {
                InsertMode.Mock => this.MockIds(records),
                InsertMode.Now => this.InsertNow(records),
                _ => Task.CompletedTask,
            };

    private Task MockIds(List<object> records)
    {
        IMockIdGenerator generator = this.template.MockIdGenerator ?? DefaultMockIdGenerator.Instance;
        PropertyInfo idField = this.template.PrimaryTargetField;
        List<object> needingIds = [.. records.Where(record => FieldState.IsUnset(idField, record))];
        _ = IdMocker.AddIds(needingIds, idField, generator);
        return Task.CompletedTask;
    }

    private Task InsertNow(List<object> records) =>
        this.context.PersistenceGateway is null
            ? throw new NotSupportedException(
                "InsertMode.Now needs a persistence gateway - RecordProvider.SetPersistenceGateway(...) - use "
                + "Mock or Never when none is configured.")
            : this.context.PersistenceGateway.Insert(records, this.template.PrimaryTargetField);
}
