using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>The parent call's state a child collection needs to generate itself against.</summary>
internal sealed record RecordProviderExecutionState(
    IProviderLocating ProviderLookup,
    IRecordProviding FactoryOutlet,
    InsertMode InsertMode,
    InsertInclusivity Inclusivity,
    IPersisting? PersistenceGateway
);