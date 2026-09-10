using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Relationships;
namespace Net.NowhereAtAll.Xfty.Lookup;

/// <summary>
/// The lookup <see cref="ProviderLookups.Of(Dictionary{ILookupKey,IRecordProvider})"/> and friends build.
/// </summary>
public sealed class MapBackedLookup(
    Dictionary<ILookupKey, Type>? providerTypeByKey,
    Dictionary<ILookupKey, IRecordProvider>? providerByKey,
    Dictionary<string, object>? sharedAncestorDefaults) : IProviderLookup, ISharedAncestorDefaults
{
    private readonly Dictionary<ILookupKey, Type>? _providerTypeByKey = providerTypeByKey;
    private readonly Dictionary<ILookupKey, IRecordProvider>? _providerByKey = providerByKey;
    private readonly Dictionary<string, object>? _sharedAncestorDefaults = sharedAncestorDefaults;
    private readonly Dictionary<ILookupKey, IRecordProvider> _instanceCache = [];

    public void RegisterSharedAncestorDefaults() =>
        this._sharedAncestorDefaults?.ToList().ForEach(pair => SharedAncestor.PutIfAbsent(pair.Key, pair.Value));

    public IRecordProvider Get(Type recordType) => this.Get(LookupKey.Get(recordType));

    public IRecordProvider Get(ILookupKey lookupKey) =>
        this._providerByKey is not null
            ? ProviderLookups.Get(this._providerByKey, lookupKey)
            : ProviderLookups.Get(this._providerTypeByKey!, this._instanceCache, lookupKey);

    public ISet<ILookupKey> KeysFor(object? record)
    {
        ISet<ILookupKey> keys = this._providerByKey is not null
            ? this._providerByKey.Keys.ToHashSet()
            : [.. this._providerTypeByKey!.Keys];
        return ProviderLookups.KeysFor(keys, record);
    }
}