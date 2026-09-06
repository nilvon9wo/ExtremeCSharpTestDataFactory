namespace Net.Nowhereatall.Xfty.FSharpAsync

open Net.Nowhereatall.Xfty.Core

/// <summary>
/// Async&lt;'T&gt; equivalents of the typed <see cref="RecordProvider{TRecord}"/>'s
/// Supply/SupplyList/SupplyBundle - see <see cref="RecordProviderAsync"/> for
/// the plain <see cref="RecordProvider"/> and why any of this exists at all.
///
/// Kept as its own module rather than more functions named <c>supply</c>/
/// <c>supplyList</c>/<c>supplyBundle</c> in the same module: F#'s let-bound
/// module functions, unlike type members, don't support ad-hoc overloading by
/// parameter type, so the two need distinct names or distinct modules -
/// distinct modules read closer to how <see cref="RecordProvider"/> and
/// <see cref="RecordProvider{TRecord}"/> are already two distinct types.
/// </summary>
module TypedRecordProviderAsync =

    /// <summary>Async&lt;'T&gt; equivalent of <see cref="RecordProvider{TRecord}.Supply"/>.</summary>
    let supply (provider: RecordProvider<'TRecord>) : Async<'TRecord> =
        provider.Supply() |> Async.AwaitTask

    /// <summary>Async&lt;'T&gt; equivalent of <see cref="RecordProvider{TRecord}.SupplyList"/>.</summary>
    let supplyList (provider: RecordProvider<'TRecord>) : Async<ResizeArray<'TRecord>> =
        provider.SupplyList() |> Async.AwaitTask

    /// <summary>Async&lt;'T&gt; equivalent of <see cref="RecordProvider{TRecord}.SupplyBundle"/>.</summary>
    let supplyBundle (provider: RecordProvider<'TRecord>) : Async<Bundle> =
        provider.SupplyBundle() |> Async.AwaitTask
