# Design: Deferred Persistence

Status: **done.** `IPersistenceGateway` (`Xfty.Persistence`) is the seam every
insert path runs through; `Xfty.EntityFrameworkCore` ships a real
implementation, proven against SQLite and (when Docker is available) a real
Postgres container in `Xfty.EntityFrameworkCore.Test`.

Two related ways to move persistence out of the per-Provider recursion:

1. **Depth-batched persistence** — the opt-in `.DepthBatched()` flag: one
   mixed-type persistence pass per dependency depth instead of one per
   Provider.
2. **The `Deferred` insert mode** — generate like `Never` over many
   `SupplyBundle()` calls, then one flush resolves the whole set with the
   same records' Ids back-filled.

Neither required rewriting `RecordFactory` — both are a structural build
(`Never`) plus a bundle-walk in `DeferredInsertBuffer`.

---

## What is built and usable today

- **`DepthBatchedInserter.ResolveAll(records, links, mode, gateway)`** — the
  Kahn-style layered algorithm: repeatedly resolve every record whose parents
  are already resolved, point their lookups at the fresh Ids (mock, or
  real via `gateway`), one layer at a time. A layer that comes up empty while
  records remain is a cycle (`CyclicGraphException`).
- **`DeferredInserter.Register(bundle)` / `.PendingCount()`** — a `Deferred`
  Provider call generates exactly like `Never` and accumulates its bundle's
  records + parent links in the static `DeferredInsertBuffer`, across every
  call.
- **`DeferredInsertBuffer.Flatten(bundle)`** — flattens one bundle's graph and
  runs the up-flow value pass (`DescendantValuePass`), so
  `CopyFromDescendantExpression` values resolve correctly, with no
  persistence attempt at all - usable with or without a configured gateway.
- **`DeferredInserter.Flush(gateway)`** — runs `DepthBatchedInserter.InsertAll`
  (hardcoded to `InsertMode.Now`) over the accumulated registry through
  `gateway`, back-filling real Ids in dependency order. Throws
  `NotSupportedException` if `gateway` is omitted.
- **`.DepthBatched()` combined with `InsertMode.Now` and a configured
  gateway** — the condition under which `RecordProvider` engages the
  depth-batched path; with any other mode, or no gateway, it is a no-op.

See `Xfty.Test/Persistence/PersistenceGatewayTest.cs` for the proof against a
mock gateway, and `Xfty.EntityFrameworkCore.Test/SqliteNowPersistenceTest.cs`
/ `PostgresNowPersistenceTest.cs` for the real-database proof.

---

## What `Deferred` never hands you, by design

It never hands a record's real Id *during* generation, even with a gateway
configured. If a later `SupplyBundle()` needs an earlier call's Id, flush the
earlier call first - flatten it, resolve/insert it, and read the Id off the
result.

---

## Relationship to other roadmap items

- **Descendant value reads** ([descendant-value-reads.md](descendant-value-reads.md))
  need the whole graph in memory before values are finalised — exactly what
  `DeferredInsertBuffer.Flatten(bundle)` gives, independent of whether a real
  flush ever runs.
- **Shared ancestors** ([shared-ancestors.md](shared-ancestors.md)) reuse the
  same depth-batched-resolution primitive for each sub-graph's pre-phase, and
  a `Deferred` main call resolves its shared ancestors up front.

Both are structurally complete and now reach full parity with the Apex
original's `Now` behaviour when a persistence gateway is configured.
