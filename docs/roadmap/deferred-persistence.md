# Design: Deferred Persistence

Status: **the graph-building side is built; the persistence side has no
backend to run against.** Condensed from the Apex original's design record —
this port made the identical structural decisions; the gap is entirely that
there is no persistence layer yet (see
[reference/known-issues](../reference/known-issues.md)).

Two related ways Apex moved DML out of the per-Provider recursion:

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

- **`DepthBatchedInserter.ResolveAll(records, links, InsertMode.Mock)`** — the
  Kahn-style layered algorithm: repeatedly resolve every record whose parents
  are already resolved, point their lookups at the fresh (mock) Ids, one
  layer at a time. A layer that comes up empty while records remain is a
  cycle (`CyclicGraphException`). Proven directly, at the algorithm level,
  without needing `Now` to work.
- **`DeferredInserter.Register(bundle)` / `.PendingCount()`** — a `Deferred`
  Provider call generates exactly like `Never` and accumulates its bundle's
  records + parent links in the static `DeferredInsertBuffer`, across every
  call. This genuinely accumulates and is fully testable.
- **`DeferredInsertBuffer.Flatten(bundle)`** — flattens one bundle's graph and
  runs the up-flow value pass (`DescendantValuePass`), so
  `CopyFromDescendantExpression` values resolve correctly, with no persistence
  attempt at all. This is the practical, working entry point for inspecting a
  deferred graph in this port.

## What always throws

- **`DeferredInserter.Flush()`** — would run `DepthBatchedInserter.InsertAll`
  (hardcoded to `InsertMode.Now`) over the accumulated registry and back-fill
  real Ids. `InsertAll` always throws `NotSupportedException` in this port —
  there is no persistence layer to insert into.
- **`.DepthBatched()` combined with `InsertMode.Now`** — the one condition
  under which `RecordProvider` actually engages the depth-batched path; with
  any other mode it is a no-op. Since `Now` throws either way, `.DepthBatched()`
  currently has **no observable effect through `RecordProvider`'s public API**.

---

## What `Deferred` never did (even in Apex) — still true here

It never hands a record's real Id *during* generation. If a later
`SupplyBundle()` needs an earlier call's Id, flush the earlier call first
(which, in this port, means: flatten it, resolve it with `Mock`, and read the
Id off the result — a real `Flush()` isn't available to do this for you yet).

---

## Relationship to other roadmap items

- **Descendant value reads** ([descendant-value-reads.md](descendant-value-reads.md))
  need the whole graph in memory before values are finalised — exactly what
  `DeferredInsertBuffer.Flatten(bundle)` gives, independent of whether a real
  flush ever runs.
- **Shared ancestors** ([shared-ancestors.md](shared-ancestors.md)) reuse the
  same depth-batched-resolution primitive for each sub-graph's pre-phase, and
  a `Deferred` main call resolves its shared ancestors up front.

Both ideas are structurally complete; both are waiting on the same missing
piece — a real persistence layer — to reach full parity with the Apex
original's `Now` behaviour.
