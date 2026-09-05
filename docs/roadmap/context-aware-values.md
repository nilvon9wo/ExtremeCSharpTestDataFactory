# Design: Context-Aware Value Generation

Status: **all three directions shipped** — sibling + ancestor reads (with a
loud ordering guard — [use/context-aware-values.md](../use/context-aware-values.md)),
and descendant (up-flowing) reads via `CopyFromDescendantExpression`
([descendant-value-reads.md](descendant-value-reads.md)). This page is the
design record, condensed from the Apex original's decision-by-decision log —
this port made the identical decisions deliberately, as a faithful port; only
the syntax and one gap (no persistence layer) differ.

Builds on `GenerationContext`
([architecture.md - The Generation Context](../contribute/architecture.md#the-generation-context)).

---

## The need

A plain value expression (`IValueExpression.Get()`) takes no arguments and
knows nothing about anything around it. Real data models routinely need more:

- **sibling read** — a field derived from another field on the *same* record;
- **ancestor read** *(down-flowing)* — a field copied down from a generated
  parent / grandparent;
- **descendant read** *(up-flowing)* — a field on a parent copied *up* from a
  generated child, so a matching-values validation stays defined once, on
  whichever record a test naturally sets it.

All three need the generation context, but differ in **timing**: a sibling's
ordering is decided by `Put` order within one value pass; an ancestor is
already fully built by the time a child's context-aware pass runs (the
factory builds relationships before values); a descendant does **not yet
exist** when its parent's value pass runs, so it needs a deferred pass.

---

## Key decisions (as shipped)

1. **`IContextAwareExpression` is a separate interface**, not a subtype of
   `IValueExpression` — a context-aware value handed to code expecting a
   plain one would violate Liskov substitution if it extended it and threw
   from the no-arg method instead. `MasterTemplate.Put(field, object?)` routes
   by runtime type: context-aware → its own map; plain → its map; a
   relationship → rejected; anything else → wrapped as `LiteralExpression`.
2. **The context exposes**, only during the per-record value pass:
   `RecordBeingBuilt` (the in-progress record, filled so far), `BundleSoFar`
   (the graph this `CreateBundle` call has produced so far — this record's
   generated relationships *and* the sibling primary records), and `RowIndex`.
   Derived via `context.ForRecord(record, bundleSoFar, rowIndex)`.
3. **Sibling ordering: two passes, `Put` order, with a loud guard.** Pass 1
   fills every plain value; pass 2 evaluates context-aware values in the
   order they were `Put`, against a record that already has all the plain
   values (and any earlier context-aware ones). `context.SiblingValue(field)`
   — which `CopyFromSiblingExpression` and any custom expression should use
   instead of reading the property directly — throws a clear
   `XftyConfigurationException` (naming both fields and the `Put` order to
   fix it) when `field` is a context-aware value not yet reached. This tells
   "not generated yet" apart from "generated to `null`": only the former
   throws. Ancestor reads are unaffected by ordering — the ancestor bundle is
   always fully built first.
4. **Descendant (up-flowing) reads need a deferred whole-graph pass** — see
   [descendant-value-reads.md](descendant-value-reads.md) for the full
   decision. In short: build the entire structure with no persistence, run an
   up-flow pass over the buffered graph once every record exists, then
   resolve. In this port that pass is `DescendantValuePass`, runnable via
   `DeferredInsertBuffer.Flatten(bundle)` even though the final persistence
   step it would normally precede (`Flush()`) always throws — see
   [deferred-persistence.md](deferred-persistence.md).

---

## Built-ins shipped

- `CopyFromSiblingExpression(field)` — `Get(ctx)` returns
  `ctx.SiblingValue(field)`.
- `CopyFromAncestorExpression(relationshipField, sourceField)` — one hop;
  `CopyFromAncestorExpression(List<PropertyInfo> path)` — multi-hop, walking
  `ctx.BundleSoFar` down each relationship.

Anything with actual transformation logic is a small consumer
`IContextAwareExpression` implementation — XFTY ships the plumbing, not a
mini-expression-language.
