using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>The parent call's state a child collection needs to generate itself against.</summary>
internal sealed record RecordProviderExecutionState(
    IProviderLookup ProviderLookup,
    IRecordProvider FactoryOutlet,
    InsertMode InsertMode,
    InsertInclusivity Inclusivity,
    IPersistenceGateway? PersistenceGateway);
