namespace Net.Nowhereatall.Xfty.FSharpAsync

open Net.Nowhereatall.Xfty.Core

/// <summary>
/// F#-idiomatic Async&lt;'T&gt; equivalents of the plain <see cref="RecordProvider"/>'s
/// Task-based Supply/SupplyList/SupplyBundle - for F# code built on the
/// original <c>async { }</c> workflow. If your code already uses the newer
/// <c>task { }</c> computation expression (built into FSharp.Core since F# 6),
/// you don't need this module at all - <c>task { }</c> consumes
/// <c>RecordProvider</c>'s own Task-returning members directly:
/// <c>task { let! result = provider.Supply() in return result }</c>.
///
/// Each function here is a thin <see cref="Async.AwaitTask"/> bridge, nothing
/// else - the awaited work, and everything it does, is identical either way.
/// <c>Async&lt;'T&gt;</c> is cold (nothing runs until something starts it -
/// <c>Async.RunSynchronously</c>, <c>Async.Start</c>, <c>Async.StartAsTask</c>),
/// unlike the <c>Task&lt;'T&gt;</c> underneath, which is already running (or
/// scheduled) the moment you hold a reference to it - that's the actual
/// reason this module exists, not just a naming preference.
/// </summary>
module RecordProviderAsync =

    /// <summary>Async&lt;'T&gt; equivalent of <see cref="RecordProvider.Supply"/>.</summary>
    let supply (provider: RecordProvider) : Async<obj> =
        provider.Supply() |> Async.AwaitTask

    /// <summary>Async&lt;'T&gt; equivalent of <see cref="RecordProvider.SupplyList"/>.</summary>
    let supplyList (provider: RecordProvider) : Async<ResizeArray<obj>> =
        provider.SupplyList() |> Async.AwaitTask

    /// <summary>Async&lt;'T&gt; equivalent of <see cref="RecordProvider.SupplyBundle"/>.</summary>
    let supplyBundle (provider: RecordProvider) : Async<Bundle> =
        provider.SupplyBundle() |> Async.AwaitTask
