namespace Net.Nowhereatall.Xfty.Core;

/// <summary>RecordProvider - building the GenerationContext for a Supply*() call, and the templates it fills.</summary>
public sealed partial class RecordProvider
{
    private GenerationContext BuildContext()
    {
        GenerationContext context = new GenerationContext(this.providerLookup, this.ContextInsertMode(), this.inclusivity)
            .WithPersistenceGateway(this.persistenceGateway)
            .WithUnsetFieldFiller(this.unsetFieldFiller)
            .WithForcedRelationshipPaths(this.templateConfig.ForcedRelationshipPaths)
            .WithPathValues(this.templateConfig.PathValues)
            .WithAncestorCycleGuard(this.ancestorCyclesAllowed)
            .WithPrimaryIdsExcluded(this.excludePrimaryIds);
        return this.BuildsStructurallyForBatchedInsert() ? context.ForBatchedInsert() : context;
    }

    private InsertMode ContextInsertMode() => this.BuildsStructurallyForBatchedInsert() ? InsertMode.Never : this.insertMode;

    private bool BuildsStructurallyForBatchedInsert() => this.FlushesGraphWhenThisCallEnds() || this.DeferredToRegistry();

    private bool FlushesGraphWhenThisCallEnds() => this.depthBatched && this.insertMode == InsertMode.Now;

    private bool DeferredToRegistry() => this.insertMode == InsertMode.Deferred;

    private List<object> TemplatesToFill()
    {
        List<object> templates = this.SuppliedOrBlankTemplates();
        return this.quantityPerListedTemplate > 1 ? MultiplyByQuantity(templates, this.quantityPerListedTemplate) : templates;
    }

    private List<object> SuppliedOrBlankTemplates() =>
        this.HasOverrideTemplates() ? this.overrideTemplateList! : [Activator.CreateInstance(this.recordType)!];

    private bool HasOverrideTemplates() => this.overrideTemplateList is { Count: > 0 };

    private void WarnIfMixingCustomTemplateWithOverrides()
    {
        if (this.templateConfig.HasCustomTemplate && this.HasOverrideTemplates())
        {
            Console.Error.WriteLine("Custom master template + overrides: overrides win all conflicts!");
        }
    }

    private static List<object> MultiplyByQuantity(List<object> templateList, int quantity) =>
        [.. Enumerable.Range(1, quantity).SelectMany(_ => templateList)];
}
