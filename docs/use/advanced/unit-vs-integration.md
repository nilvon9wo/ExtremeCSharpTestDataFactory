# Unit vs Integration Tests, One Set of Providers

XFTY's point: the *same* Provider definitions should serve both isolated unit
tests and database integration tests, with only the
[insert mode](../insert-modes.md) changing.

> **This port has no persistence layer yet, so today there is no integration
> side to flip to** — `InsertMode.Now` always throws `NotSupportedException`
> (see [insert-modes](../insert-modes.md)). This page describes the design
> intent the Apex original demonstrates and that this port's API is shaped to
> preserve: when a real persistence layer is wired up, promoting a test should
> still be a one-line change, not a rewrite of its setup. Everything below that
> depends on an actual database running is marked as such.

---

## The shared shape

```csharp
private static readonly DefaultProviderLookup Lookup = new();
```

A unit test, today:

```csharp
Contact generatedContact = (Contact)new RecordProvider(typeof(Contact), Lookup)
    .SetInsertMode(InsertMode.Mock)
    .SetInclusivity(InsertInclusivity.Required)
    .Supply();
```

The integration-test line this is designed to become, once a persistence layer
exists:

```csharp
    .SetInsertMode(InsertMode.Now)
```

- `Mock` + `Required` — no persistence, realistic Ids, valid required data,
  compact graphs. The only usable starting point today.
- `Now` + `Required` — the same graph, actually persisted. Not yet possible.

Because the data *description* does not change, a test built this way should be
promotable from unit to integration (or the reverse) without touching its
setup, once `Now` is real.

---

## What "usually" will be carrying, once `Now` works

The flip is only free when the graph can actually be persisted. Some of Apex's
four caveats here are Salesforce-specific and will not recur; two are general
enough to expect again against any real backend:

1. **A Provider is only as correct as its author kept it.** `Mock` never runs
   validation logic a real save would; a Provider whose data has drifted behind
   real constraints would pass every `Mock` test and fail the moment the same
   test ran for real. The fix belongs in [the Provider](../../extend/providers.md),
   not the test.

2. **Values a test forced in that a real save cannot set.** [`Inject` /
   `InjectAll`](../enrichment.md) and [`SObjectInjector`](../sobject-injector.md)
   write things a real save would compute differently or reject outright — a
   populated navigation property that is really a snapshot, a forced value on a
   field a real backend treats as read-only or system-managed. Under `Mock`
   those stick and the assertions pass; against a real backend the same fields
   would come back different, and the test would fail in the *opposite*
   direction: green under `Mock`, red for real. Treat an injected graph as
   read-only input to a `Mock` unit test and nothing else.

Salesforce-specific caveats that do not apply to this port at all: object types
that cannot be inserted from Apex, and mixed-DML restrictions across setup
objects — neither concept exists outside a Salesforce org.

The takeaway: default to `Mock`, and design Providers so that when `Now` is
real, it stays a one-line change — not a switch that is guaranteed to stay
flipped without its own verification.
