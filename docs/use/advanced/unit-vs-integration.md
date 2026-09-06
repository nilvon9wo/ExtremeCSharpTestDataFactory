# Unit vs Integration Tests, One Set of Providers

The same Provider definitions serve both an isolated unit test and a real
database integration test — only the [insert mode](../insert-modes.md) (and,
for `Now`, the configured `IPersistenceGateway`) changes.

---

## The shared shape

```csharp
private static readonly DefaultProviderLookup Lookup = new();
```

A unit test:

```csharp
Contact generatedContact = (Contact)await new RecordProvider(typeof(Contact), Lookup)
    .SetInsertMode(InsertMode.Mock)
    .SetInclusivity(InsertInclusivity.Required)
    .Supply();
```

The integration-test version of the same test:

```csharp
    .SetPersistenceGateway(gateway)
    .SetInsertMode(InsertMode.Now)
```

- `Mock` + `Required` — no persistence, realistic Ids, valid required data,
  compact graphs. The default for most tests.
- `Now` + `Required` (with a gateway configured) — the same graph, actually
  persisted through it. See `Xfty.EntityFrameworkCore.Test` for this proven
  against a real SQLite database and (when Docker is available) a real
  Postgres container.

Because the data *description* does not change, a test built this way is
promotable from unit to integration (or the reverse) without touching its
setup - just the insert mode and gateway.

---

## Where the flip stops being free

The flip is only free when the graph can actually be persisted as described.
Two things break that:

1. **A Provider is only as correct as its author kept it.** `Mock` never runs
   validation logic a real save would; a Provider whose data has drifted behind
   real constraints would pass every `Mock` test and fail the moment the same
   test ran for real. The fix belongs in [the Provider](../../extend/providers.md),
   not the test.

2. **Values a test forced in that a real save cannot set.** [`Inject` /
   `InjectAll`](../enrichment.md) and [`RecordInjector`](../record-injector.md)
   write things a real save would compute differently or reject outright — a
   populated navigation property that is really a snapshot, a forced value on a
   field a real backend treats as read-only or system-managed. Under `Mock`
   those stick and the assertions pass; against a real backend the same fields
   would come back different, and the test would fail in the *opposite*
   direction: green under `Mock`, red for real. Treat an injected graph as
   read-only input to a `Mock` unit test and nothing else.

The takeaway: default to `Mock`, and design Providers so that flipping to
`Now` against a real gateway stays a one-line change — not a switch that is
guaranteed to stay working without its own verification.

Runnable: `RecordFactoryTest`, `RecordProviderIntegrationTest`, `PersistenceGatewayTest`
