using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>This test's own tiny Provider Lookup, registering only <see cref="DocumentEmbedding"/>.</summary>
public sealed class DocumentEmbeddingProviderLookup : IProviderLocating
{
    private static readonly Dictionary<IRecordIdentifying, Type> ProviderTypeByKey = new()
    {
        [LookupKey.Get<DocumentEmbedding>()] = typeof(DocumentEmbeddingProvider),
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