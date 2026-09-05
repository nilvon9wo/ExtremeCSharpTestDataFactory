using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Demo;

/// <summary>
/// This port's own Provider Lookup - the Account / Contact Providers it
/// ships - used by its own tests and offered as a starter kit. A mechanical
/// port of Apex's XFTY_DefaultSObjectProviderLookup, minus the User Provider:
/// no C# analog exists for a Salesforce org's User/Profile/UserRole schema
/// or a live DML insert to seed an admin user (see csharp-port-idea.md).
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

    public IRecordProvider Get(Type sObjectType) => this.Get(LookupKey.Get(sObjectType));

    public IRecordProvider Get(ILookupKey lookupKey) => ProviderLookups.Get(ProviderTypeByKey, this.instanceCache, lookupKey);

    public ISet<ILookupKey> KeysFor(object? record) => ProviderLookups.KeysFor(ProviderTypeByKey.Keys.ToHashSet(), record);
}
