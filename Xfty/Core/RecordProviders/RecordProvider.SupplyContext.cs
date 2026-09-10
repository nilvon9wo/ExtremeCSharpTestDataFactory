namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>RecordProvider - building the GenerationContext for a Supply*() call, and the templates it fills.</summary>
public sealed partial class RecordProvider
{
    private GenerationContext BuildContext()
    {
        GenerationContext context = new GenerationContext(this._providerLookup, this.ContextInsertMode(), this._inclusivity)
            .WithPersistenceGateway(this._persistenceGateway)
            .WithUnsetFieldFiller(this._unsetFieldFiller)
            .WithForcedRelationshipPaths(this._templateConfig.ForcedRelationshipPaths)
            .WithPathValues(this._templateConfig.PathValues)
            .WithAncestorCycleGuard(this._ancestorCyclesAllowed)
            .WithPrimaryIdsExcluded(this._excludePrimaryIds);
        return this.BuildsStructurallyForBatchedInsert() ? context.ForBatchedInsert() : context;
    }

    private InsertMode ContextInsertMode() => this.BuildsStructurallyForBatchedInsert() ? InsertMode.Never : this._insertMode;

    private bool BuildsStructurallyForBatchedInsert() => this.FlushesGraphWhenThisCallEnds() || this.DeferredToRegistry();

    private bool FlushesGraphWhenThisCallEnds() => this._depthBatched && this._insertMode == InsertMode.Now;

    private bool DeferredToRegistry() => this._insertMode == InsertMode.Deferred;

    private List<object> TemplatesToFill()
    {
        List<object> templates = this.SuppliedOrBlankTemplates();
        return this._quantityPerListedTemplate > 1 ? MultiplyByQuantity(templates, this._quantityPerListedTemplate) : templates;
    }

    private List<object> SuppliedOrBlankTemplates() =>
        this.HasOverrideTemplates() ? this._overrideTemplateList! : [BlankInstances.Of(this._recordType)];

    private bool HasOverrideTemplates() => this._overrideTemplateList is { Count: > 0 };

    private void WarnIfMixingCustomTemplateWithOverrides()
    {
        if (this._templateConfig.HasCustomTemplate && this.HasOverrideTemplates())
        {
            Console.Error.WriteLine("Custom master template + overrides: overrides win all conflicts!");
        }
    }

    private static List<object> MultiplyByQuantity(List<object> templateList, int quantity) =>
        [.. Enumerable.Range(1, quantity).SelectMany(_ => templateList)];
}