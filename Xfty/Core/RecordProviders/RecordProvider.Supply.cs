using Net.NowhereAtAll.Xfty.Core.Bundles;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// RecordProvider - the terminal Supply*() calls: snapshot the configuration into a <see cref="RecordProviderPlan"/>
/// and run it.
/// </summary>
public sealed partial class RecordProvider
{
    public Task<Bundle> SupplyBundle() => this.Execution().SupplyBundle();

    public Task<List<object>> SupplyList() => this.Execution().SupplyList();

    public Task<object> Supply() => this.Execution().Supply();

    private RecordProviderExecution Execution() => new(this.Snapshot());

    private RecordProviderPlan Snapshot() =>
        new(
            this._providerLookup,
            this._recordType,
            this.ResolveFactoryOutlet(),
            this._insertMode,
            this._inclusivity,
            this._quantityPerListedTemplate,
            this._overrideTemplateList,
            this._persistenceGateway,
            this._unsetFieldFiller,
            this._ancestorCyclesAllowed,
            this._excludePrimaryIds,
            this._depthBatched,
            this._forceStructuralChildGeneration,
            this._templateConfig,
            this._childConfig);
}