using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Relationships;
namespace Net.Nowhereatall.Xfty.Lookup;

/// <summary>The lookup <see cref="ProviderLookups.Of(Dictionary{ILookupKey,IRecordProvider})"/> and friends build.</summary>
public sealed class MapBackedLookup(
    Dictionary<ILookupKey, Type>? providerTypeByKey,
    Dictionary<ILookupKey, IRecordProvider>? providerByKey,
    Dictionary<string, object>? sharedAncestorDefaults) : IProviderLookup, ISharedAncestorDefaults
{
    private readonly Dictionary<ILookupKey, Type>? providerTypeByKey = providerTypeByKey;
    private readonly Dictionary<ILookupKey, IRecordProvider>? providerByKey = providerByKey;
    private readonly Dictionary<string, object>? sharedAncestorDefaults = sharedAncestorDefaults;
    private readonly Dictionary<ILookupKey, IRecordProvider> instanceCache = [];

    public void RegisterSharedAncestorDefaults() =>
        this.sharedAncestorDefaults?.ToList().ForEach(pair => SharedAncestor.PutIfAbsent(pair.Key, pair.Value));

    public IRecordProvider Get(Type recordType) => this.Get(LookupKey.Get(recordType));

    public IRecordProvider Get(ILookupKey lookupKey) =>
        this.providerByKey is not null
            ? ProviderLookups.Get(this.providerByKey, lookupKey)
            : ProviderLookups.Get(this.providerTypeByKey!, this.instanceCache, lookupKey);

    public ISet<ILookupKey> KeysFor(object? record)
    {
        ISet<ILookupKey> keys = this.providerByKey is not null
            ? this.providerByKey.Keys.ToHashSet()
            : [.. this.providerTypeByKey!.Keys];
        return ProviderLookups.KeysFor(keys, record);
    }
}
