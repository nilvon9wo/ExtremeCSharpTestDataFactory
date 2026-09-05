# Deferred & Depth-Batched Insert

Two ways Apex moves DML out of the per-Provider recursion. **Neither actually
persists anything in this port** — there is no database to insert into (see
[insert-modes](insert-modes.md)) — but the generation and graph-flattening
machinery behind both is fully ported and testable on its own.

---

## `Deferred` — generate over many calls, register instead of inserting

```csharp
Bundle accounts = new RecordProvider(typeof(Account), lookup)
    .SetInsertMode(InsertMode.Deferred)
    .SetQuantityPerTemplate(3)
    .SupplyBundle();

Bundle contacts = new RecordProvider(typeof(Contact), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Deferred)
    .SupplyBundle();

DeferredInserter.Flush();   // throws NotSupportedException - no persistence layer to flush into
```

`Deferred` generates exactly like `Never` — no Ids — but registers every record
with `DeferredInserter`, the same static registry Apex's `flush()` reads from.
`DeferredInserter.PendingCount()` genuinely accumulates across every
`Register()` call, proving the registration side works; `Flush()` always throws
`NotSupportedException`, because inserting is the one part with no C# analog
yet.

- A test that never calls `Flush()` gets `Never` semantics — no surprise
  behaviour.
- A failed `Flush()` does not silently lose what was registered — the registry
  only clears after a successful insert, which never happens here.

### Inspecting the resolved graph without persisting

The piece that *is* fully usable is flattening a deferred graph and running its
value resolution — including
[`CopyFromDescendantExpression`](context-aware-values.md#reading-up-from-a-child)
— without ever trying to insert:

```csharp
DeferredInsertBuffer graph = DeferredInsertBuffer.Flatten(bundle);
// graph.Records() / graph.ParentLinks() - the flattened graph, up-flow values already resolved

graph.ResolveAll(InsertMode.Mock);   // assigns mock Ids in dependency order, same algorithm Now would use
```

`DeferredInsertBuffer` and `DepthBatchedInserter` (below) live in
`Net.Nowhereatall.Xfty.Persistence` and are the lower-level pieces
`RecordProvider` builds on. Reach for them directly when a test wants to prove
the graph is well-formed (parent-before-child ordering, shared ancestors
collapsed to one row, up-flow values resolved) without a working `Now`.

---

## `.DepthBatched()` — one persistence pass per depth, under `Now` only

By default `Now` would run one insert per Provider. `.DepthBatched()` collapses
that to one pass per dependency depth:

```csharp
new RecordProvider(typeof(Case), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Now)
    .DepthBatched()
    .SupplyBundle();
```

> **`.DepthBatched()` only changes anything when combined with `InsertMode.Now`**
> — and `Now` always throws in this port. With any other insert mode,
> `RecordProvider` ignores `.DepthBatched()` entirely (it is wired to the same
> condition that decides whether to flush a deferred graph). There is currently
> no way to observe `.DepthBatched()`'s effect through `RecordProvider` itself;
> the underlying algorithm is proven directly against
> `DepthBatchedInserter.ResolveAll(records, parentLinks, InsertMode.Mock)`
> instead — see `Xfty.Test/Persistence/DepthBatchedInserterTest.cs`. See
> [reference/known-issues.md](../reference/known-issues.md).

- Shared ancestors and `CopyFromDescendantExpression` values both resolve
  correctly ahead of the batched step — the whole graph exists in memory first.
- A lookup cycle (A → B, B → A) cannot be resolved in dependency order and
  throws `CyclicGraphException`.

See also: [insert-modes](insert-modes.md) · [advanced/deep-setup-chains](advanced/deep-setup-chains.md)

Runnable: `DeferredInserterTest`, `DeferredInsertBufferTest`, `DepthBatchedInserterTest`
