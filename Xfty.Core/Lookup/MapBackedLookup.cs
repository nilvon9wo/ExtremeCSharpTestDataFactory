using Net.Nowhereatall.Xfty.Core.Core;
using Net.Nowhereatall.Xfty.Core.Engine;
namespace Net.Nowhereatall.Xfty.Core.Lookup;

/// <summary>The lookup <see cref="ProviderLookups.Of(Dictionary{ILookupKey,IRecordProvider})"/> and friends build.</summary>
public sealed class MapBackedLookup : IProviderLookup, ISharedAncestorDefaults
{
    private readonly Dictionary<ILookupKey, Type>? providerTypeByKey;
    private readonly Dictionary<ILookupKey, IRecordProvider>? providerByKey;
    private readonly Dictionary<string, object>? sharedAncestorDefaults;
    private readonly Dictionary<ILookupKey, IRecordProvider> instanceCache = [];

    public MapBackedLookup(
        Dictionary<ILookupKey, Type>? providerTypeByKey,
        Dictionary<ILookupKey, IRecordProvider>? providerByKey,
        Dictionary<string, object>? sharedAncestorDefaults)
    {
        this.providerTypeByKey = providerTypeByKey;
        this.providerByKey = providerByKey;
        this.sharedAncestorDefaults = sharedAncestorDefaults;
    }

    // SharedAncestor/relationships/ (and the depth-batched persistence it needs to
    // resolve against) is not ported yet - see csharp-port-idea.md. Only a lookup
    // actually constructed with shared-ancestor defaults would hit this.
    public void RegisterSharedAncestorDefaults() =>
        _ = this.sharedAncestorDefaults is not null && this.sharedAncestorDefaults.Count > 0
            ? throw new NotSupportedException("Shared ancestors are not ported to C# yet.")
            : true;

    public IRecordProvider Get(Type sObjectType) => this.Get(LookupKey.Get(sObjectType));

    public IRecordProvider Get(ILookupKey lookupKey) =>
        this.providerByKey is not null
            ? ProviderLookups.Get(this.providerByKey, lookupKey)
            : ProviderLookups.Get(this.providerTypeByKey!, this.instanceCache, lookupKey);

    public ISet<ILookupKey> KeysFor(object? record)
    {
        ISet<ILookupKey> keys = this.providerByKey is not null
            ? this.providerByKey.Keys.ToHashSet()
            : this.providerTypeByKey!.Keys.ToHashSet();
        return ProviderLookups.KeysFor(keys, record);
    }
}
