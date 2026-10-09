using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Demo;

/// <summary>
/// This library's own bundled Provider Lookup - the Account / Contact
/// Providers it ships - used by its own tests and offered as a starter kit.
///
/// Do not edit this class for your own project - copy it, swap the map
/// entries for your own Providers, and pass your class to
/// <c>new RecordProvider(type, new MyProjectLookup())</c>.
/// </summary>
public sealed class DefaultProviderLookup : IProviderLocating
{
    private static readonly Dictionary<IRecordIdentifying, Type> ProviderTypeByKey = new()
    {
        [LookupKey.Get<Account>()] = typeof(AccountDataProvider),
        [LookupKey.Get<Contact>()] = typeof(ContactDataProvider),
    };

    private readonly Dictionary<IRecordIdentifying, IRecordProviding> _instanceCache = [];

    public IRecordProviding Get(Type recordType) => this.Get(LookupKey.Get(recordType));

    public IRecordProviding Get(IRecordIdentifying lookupKey) =>
        ProviderLookups.Get(
            ProviderTypeByKey,
            this._instanceCache,
            lookupKey
        );

    public ISet<IRecordIdentifying> KeysFor(object? record) =>
        ProviderLookups.KeysFor(ProviderTypeByKey.Keys.ToHashSet(), record);
}