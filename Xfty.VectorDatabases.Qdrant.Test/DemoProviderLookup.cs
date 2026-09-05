using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>This test's own tiny Provider Lookup, registering only <see cref="DocumentChunk"/>.</summary>
public sealed class DemoProviderLookup : IProviderLookup
{
    private static readonly Dictionary<ILookupKey, Type> ProviderTypeByKey = new()
    {
        [LookupKey.Get(typeof(DocumentChunk))] = typeof(DocumentChunkProvider),
    };

    private readonly Dictionary<ILookupKey, IRecordProvider> instanceCache = [];

    public IRecordProvider Get(Type recordType) => this.Get(LookupKey.Get(recordType));

    public IRecordProvider Get(ILookupKey lookupKey) => ProviderLookups.Get(ProviderTypeByKey, this.instanceCache, lookupKey);

    public ISet<ILookupKey> KeysFor(object? record) => ProviderLookups.KeysFor(ProviderTypeByKey.Keys.ToHashSet(), record);
}
