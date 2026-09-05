using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>The parent call's state a child collection needs to generate itself against.</summary>
internal sealed record RecordProviderExecutionState(
    IProviderLookup ProviderLookup,
    IRecordProvider FactoryOutlet,
    InsertMode InsertMode,
    InsertInclusivity Inclusivity,
    IPersistenceGateway? PersistenceGateway);
