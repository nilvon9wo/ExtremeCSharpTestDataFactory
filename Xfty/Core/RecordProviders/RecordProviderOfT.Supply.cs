using Net.NowhereAtAll.Xfty.Core.Bundles;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>RecordProvider&lt;TRecord&gt; - the terminal Supply*() calls, typed to <typeparamref name="TRecord"/> so the caller needs no cast.</summary>
public sealed partial class RecordProvider<TRecord>
{
    public async Task<TRecord> Supply() =>
        (TRecord)await this._inner.Supply().ConfigureAwait(false);

    public async Task<List<TRecord>> SupplyList() =>
        [.. (await this._inner.SupplyList().ConfigureAwait(false)).Cast<TRecord>()];

    public Task<Bundle> SupplyBundle() => this._inner.SupplyBundle();
}