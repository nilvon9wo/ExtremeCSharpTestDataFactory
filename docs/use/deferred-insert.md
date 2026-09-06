# Deferred & Depth-Batched Insert

Two ways to move persistence out of the per-Provider recursion: build the
whole graph in memory across several calls, then insert it once, in
dependency order, regardless of which Provider originally generated each
record.

---

## `Deferred` — generate over many calls, register instead of inserting

```csharp
Bundle accounts = await new RecordProvider(typeof(Account), lookup)
    .SetInsertMode(InsertMode.Deferred)
    .SetQuantityPerTemplate(3)
    .SupplyBundle();

Bundle contacts = await new RecordProvider(typeof(Contact), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Deferred)
    .SupplyBundle();

await DeferredInserter.Flush(gateway);   // one pass, in dependency order, through the given IPersistenceGateway
```

`Deferred` generates exactly like `Never` — no Ids — but registers every
record with `DeferredInserter`'s static registry instead. `Flush(gateway)`
resolves the whole registered set in dependency order and inserts it through
`gateway`; call `Flush()` with no gateway and it throws `NotSupportedException`
rather than silently doing nothing.

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
`Net.NowhereAtAll.Xfty.Persistence` and are the lower-level pieces
`RecordProvider` builds on. Reach for them directly when a test wants to prove
the graph is well-formed (parent-before-child ordering, shared ancestors
collapsed to one row, up-flow values resolved) without a working `Now`.

---

## `.DepthBatched()` — one persistence pass per depth, under `Now` only

By default `Now` would run one insert per Provider. `.DepthBatched()` collapses
that to one pass per dependency depth:

```csharp
await new RecordProvider(typeof(Case), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Now)
    .DepthBatched()
    .SupplyBundle();
```

> **`.DepthBatched()` only changes anything when combined with `InsertMode.Now`**
> and a configured `.SetPersistenceGateway(...)` - with any other insert mode,
> or with no gateway, `RecordProvider` ignores it (it is wired to the same
> condition that decides whether to flush a deferred graph). With both in
> place it genuinely runs one `gateway.Insert(...)` call per dependency depth
> instead of one per Provider - see
> `Xfty.Test/Persistence/PersistenceGatewayTest.cs` and
> `Xfty.EntityFrameworkCore.Test/SqliteNowPersistenceTest.cs` for the proof
> against a mock and a real database respectively. The underlying layering
> algorithm is also proven directly against
> `DepthBatchedInserter.ResolveAll(records, parentLinks, InsertMode.Mock)` -
> see `Xfty.Test/Persistence/DepthBatchedInserterTest.cs`.

- Shared ancestors and `CopyFromDescendantExpression` values both resolve
  correctly ahead of the batched step — the whole graph exists in memory first.
- A lookup cycle (A → B, B → A) cannot be resolved in dependency order and
  throws `CyclicGraphException`.

See also: [insert-modes](insert-modes.md) · [advanced/deep-setup-chains](advanced/deep-setup-chains.md)

Runnable: `DeferredInserterTest`, `DeferredInsertBufferTest`, `DepthBatchedInserterTest`, `PersistenceGatewayTest`
