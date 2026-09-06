# Xfty.FSharpAsync

F#-idiomatic `Async<'T>` wrappers over [`Xfty`](https://www.nuget.org/packages/Xfty)'s
`Task`-based API, for F# code built on the original `async { }` workflow.

```bash
dotnet add package Xfty.FSharpAsync
```

## Do you need this at all?

Only if your code uses F#'s original `async { }` computation expression. If
you're on the newer `task { }` (built into `FSharp.Core` since F# 6), you
don't need this package - `task { }` consumes `Xfty`'s `Task`-returning
members directly, with no wrapper:

```fsharp
task {
    let! contact = provider.Supply()
    return contact
}
```

`Async<'T>` and `Task<'T>` aren't just syntactically different: a `Task<'T>`
is **hot** - the work is already running (or scheduled) the instant you hold
a reference to it - while an `Async<'T>` is **cold** - nothing happens until
something explicitly starts it (`Async.RunSynchronously`, `Async.Start`,
`Async.StartAsTask`). If your codebase relies on that cold-start semantics,
or is simply built on `async { }` throughout, this package bridges the gap.

## Usage

```fsharp
open Net.Nowhereatall.Xfty.Core
open Net.Nowhereatall.Xfty.FSharpAsync

async {
    let provider =
        RecordProvider(typeof<Contact>, lookup)
            .SetInsertMode(InsertMode.Mock)

    let! contact = RecordProviderAsync.supply provider
    return contact
}
```

| Module | Wraps |
|---|---|
| `RecordProviderAsync` | The plain `RecordProvider`'s `Supply`/`SupplyList`/`SupplyBundle` |
| `TypedRecordProviderAsync` | The typed `RecordProvider<'TRecord>`'s `Supply`/`SupplyList`/`SupplyBundle` - kept as its own module rather than same-named overloads, since F#'s `let`-bound module functions don't support ad-hoc overloading by parameter type the way type members do |
| `DeferredInserterAsync` | `DeferredInserter.Flush` - takes an `IPersistenceGateway option` rather than a nullable reference, matching F#'s own idiom |

Every function is a thin `Async.AwaitTask` bridge - the awaited work, and
everything it does, is identical to calling the wrapped `Xfty` member
directly.

## Full documentation

- [`Xfty`](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory#readme) - the core library this wraps
