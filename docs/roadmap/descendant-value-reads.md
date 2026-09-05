# Roadmap: Descendant (Up-Flowing) Value Reads

Status: **✅ built** (option B, below). `CopyFromDescendantExpression`,
resolved in a pass over the whole deferred forest before persistence would
run. This was decision 4 of [context-aware-values.md](context-aware-values.md);
usage in [../use/context-aware-values.md](../use/context-aware-values.md#reading-up-from-a-child).

Implemented:

- `IDeferredExpression` — a value read up from a descendant; its own template
  slot (`DeferredExpressionByField`), so the normal value passes ignore it.
- `CopyFromDescendantExpression(childLookupField, sourceField)` — copies a
  field from the child that references this record through
  `childLookupField`; first matching child, or `null`.
- `RecordFactory` leaves the field unresolved and calls `bundle.DeferValues(...)`;
  **in any mode but `Deferred` / `.DepthBatched()` it throws** — not a silent
  `null`.
- `DeferredInsertBuffer` captures each pending value keyed by the record's
  flat index; `DescendantValuePass` (via `DeferredGraph.ChildrenOf`) fills
  them at the top of `Flatten(bundle)` / `ResolveAll(mode)`, before any
  persistence would run.
- Works for a generated ancestor reading its requesting child **and** for a
  parent reading one of its `WithChildren` rows.

Not built: a multi-hop path form (`CopyFromAncestorExpression` has one);
reading an **aggregate** across many children (only the first is read); a
loud error when a deferred build registers one but the graph is never
flattened (the value stays `null`).

---

## The need

[Context-aware values](../use/context-aware-values.md) read *down* the tree
(from a generated ancestor) and *sideways* (a sibling on the same record).
Reading *up* — a **parent** field derived from a generated **child** — cannot
ride the same pass, because the child does not exist when the parent is
built.

---

## Decision: option B (a pass inside the deferred flush/flatten), not option A

**Option B** — a value pass run when the whole deferred forest is
buffered (`DeferredInsertBuffer`). It already accumulates every record before
resolving, so a pass over those buffered records first lets an up-flowing
expression read any descendant.

**Option A** (rejected) — a light `context.RequestingChildTemplate`, covering
only "a matching value the test set explicitly on the one requesting child",
in any insert mode. Narrower, and would have doubled the surface for a
feature option B already fully covers.

### The constraint this imposes

Up-flowing reads require `Deferred` mode (or `.DepthBatched()`). A test that
needs one and is not using either gets a clear error, not a silent `null`. In
this port, resolving it does not require a working `Flush()` — it works
directly against `DeferredInsertBuffer.Flatten(bundle)` (see
[deferred-persistence.md](deferred-persistence.md)).
