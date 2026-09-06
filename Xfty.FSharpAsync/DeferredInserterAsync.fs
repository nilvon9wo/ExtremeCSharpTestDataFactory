namespace Net.Nowhereatall.Xfty.FSharpAsync

open Net.Nowhereatall.Xfty.Persistence

/// <summary>
/// Async&lt;'T&gt; equivalent of <see cref="DeferredInserter.Flush"/> - see
/// <see cref="RecordProviderAsync"/> for why this package exists at all.
/// </summary>
module DeferredInserterAsync =

    /// <summary>
    /// Async&lt;'T&gt; equivalent of <see cref="DeferredInserter.Flush"/>.
    /// <paramref name="gateway"/> is an F# option rather than a nullable
    /// reference, matching F#'s own idiom - <c>None</c> flushes with no
    /// gateway configured (throwing, same as the C# default parameter would),
    /// <c>Some gateway</c> flushes through it.
    /// </summary>
    let flush (gateway: IPersistenceGateway option) : Async<unit> =
        (match gateway with
         | Some g -> DeferredInserter.Flush(g)
         | None -> DeferredInserter.Flush())
        |> Async.AwaitTask
