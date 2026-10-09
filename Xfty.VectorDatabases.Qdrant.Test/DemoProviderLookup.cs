using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>This test's own tiny Provider Lookup, registering only <see cref="DocumentChunk"/>.</summary>
public sealed class DemoProviderLookup : IProviderLocating
{
    private static readonly Dictionary<IRecordIdentifying, Type> ProviderTypeByKey = new()
    {
        [LookupKey.Get<DocumentChunk>()] = typeof(DocumentChunkProvider),
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