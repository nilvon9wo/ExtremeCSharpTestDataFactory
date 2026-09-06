using Net.NowhereAtAll.Xfty.Core;
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
public sealed class DefaultProviderLookup : IProviderLookup
{
    private static readonly Dictionary<ILookupKey, Type> ProviderTypeByKey = new()
    {
        [LookupKey.Get(typeof(Account))] = typeof(AccountDataProvider),
        [LookupKey.Get(typeof(Contact))] = typeof(ContactDataProvider),
    };

    private readonly Dictionary<ILookupKey, IRecordProvider> instanceCache = [];

    public IRecordProvider Get(Type recordType) => this.Get(LookupKey.Get(recordType));

    public IRecordProvider Get(ILookupKey lookupKey) => ProviderLookups.Get(ProviderTypeByKey, this.instanceCache, lookupKey);

    public ISet<ILookupKey> KeysFor(object? record) => ProviderLookups.KeysFor(ProviderTypeByKey.Keys.ToHashSet(), record);
}
