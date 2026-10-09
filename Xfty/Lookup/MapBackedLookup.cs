using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Relationships;
namespace Net.NowhereAtAll.Xfty.Lookup;

/// <summary>
/// The lookup <see cref="ProviderLookups.Of(Dictionary{IRecordIdentifying,IRecordProviding})"/> and friends build.
/// </summary>
public sealed class MapBackedLookup(
    Dictionary<IRecordIdentifying, Type>? providerTypeByKey,
    Dictionary<IRecordIdentifying, IRecordProviding>? providerByKey,
    Dictionary<string, object>? sharedAncestorDefaults
) : IProviderLocating, ISharedAncestorRegistering
{
    private readonly Dictionary<IRecordIdentifying, Type>? _providerTypeByKey = providerTypeByKey;
    private readonly Dictionary<IRecordIdentifying, IRecordProviding>? _providerByKey = providerByKey;
    private readonly Dictionary<string, object>? _sharedAncestorDefaults = sharedAncestorDefaults;
    private readonly Dictionary<IRecordIdentifying, IRecordProviding> _instanceCache = [];

    public void RegisterSharedAncestorDefaults() =>
        this._sharedAncestorDefaults?.ToList().ForEach(pair => SharedAncestor.PutIfAbsent(pair.Key, pair.Value));

    public IRecordProviding Get(Type recordType) => this.Get(LookupKey.Get(recordType));

    public IRecordProviding Get(IRecordIdentifying lookupKey) =>
        this._providerByKey is not null
            ? ProviderLookups.Get(this._providerByKey, lookupKey)
            : ProviderLookups.Get(this._providerTypeByKey!, this._instanceCache, lookupKey);

    public ISet<IRecordIdentifying> KeysFor(object? record)
    {
        ISet<IRecordIdentifying> keys = this._providerByKey is not null
            ? this._providerByKey.Keys.ToHashSet()
            : [.. this._providerTypeByKey!.Keys];
        return ProviderLookups.KeysFor(keys, record);
    }
}