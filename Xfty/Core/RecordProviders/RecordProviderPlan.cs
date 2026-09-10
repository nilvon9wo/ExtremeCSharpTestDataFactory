using System.Reflection;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// An immutable snapshot of everything a single Supply*() call needs: what to
/// generate, how many, into which insert mode, and the two config collaborators
/// (<see cref="RecordProviderTemplateConfig"/> / <see cref="RecordProviderChildConfig"/>),
/// which are frozen by the time a call runs. <see cref="RecordProvider"/> builds
/// one and hands it to <see cref="RecordProviderExecution"/>.
/// </summary>
internal sealed record RecordProviderPlan(
    IProviderLookup ProviderLookup,
    Type RecordType,
    IRecordProvider Outlet,
    InsertMode InsertMode,
    InsertInclusivity Inclusivity,
    int QuantityPerTemplate,
    List<object>? OverrideTemplates,
    IPersistenceGateway? PersistenceGateway,
    IUnsetFieldFiller? UnsetFieldFiller,
    bool AncestorCyclesAllowed,
    bool ExcludePrimaryIds,
    bool DepthBatched,
    bool ForceStructuralChildGeneration,
    RecordProviderTemplateConfig TemplateConfig,
    RecordProviderChildConfig ChildConfig)
{
    public PropertyInfo PrimaryTargetField => this.Outlet.PrimaryTargetField;

    public bool HasOverrideTemplates => this.OverrideTemplates is { Count: > 0 };
}