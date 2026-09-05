# Design: Shared Ancestors

Status: **implemented, one API, resolution auto-detected.** Usage in
[use/shared-ancestors.md](../use/shared-ancestors.md). Condensed from the Apex
original's much longer decision log — the shipped design is the same; this
page keeps the durable rationale and drops the multi-stage historical debate
that arrived at it.

---

## Requirements

1. **Deterministic, not pooled.** A shared ancestor is *one* record. Every
   field that references it gets the same Id.
2. **Named and interned.** `SharedAncestor.Get("john")` returns the one
   instance for that name, from anywhere.
3. **Built at most once per process run** (see the static-lifetime note
   below — this is where this port genuinely diverges from Apex). Later
   references reuse the resolved record.
4. **Cross-template, cross-type.** The same `SharedAncestor.Get("john")` can
   sit in more than one Master Template's relationship slot.
5. **Fills either slot.** It implements `IDefaultRelationship`, so it goes in
   `PutRequired(...)` or `PutOptional(...)` like any relationship.
6. **Resolution-frugal.** Resolving N shared ancestors must not cost N
   separate resolution passes when they share a dependency chain.

```csharp
// somewhere central
SharedAncestor.Put("acme-hq", new Account { Name = "ACME HQ" });

// any Master Template, any field
.PutRequired(Field.Of<Contact>(x => x.AccountId), SharedAncestor.Get("acme-hq"))
```

---

## The registry, and the one place this port genuinely differs from Apex

`SharedAncestor` interns instances through a `static Dictionary<string, SharedAncestor>`.

**Apex's design rationale here assumed "no reset hook is needed, because
Salesforce isolates every test method — static state never survives from one
test method to the next." That assumption is simply false for a shared xUnit
test process**, where a `static` field lives for the life of the process. This
was not a hypothetical risk: it caused real cross-test contamination during
this port's own development (a shared ancestor left registered-but-unresolved
by one test poisoned every later test's resolution pre-phase). See
[reference/salesforce-considerations](../reference/salesforce-considerations.md)
for the full account and the naming/`Disable(name)` convention this port's own
test suite now follows as mitigation, since there is no language-level fix —
it is inherent to how .NET statics work.

---

## Resolution — auto-detected, one pre-phase

Before a Provider generates anything, every shared ancestor configured so far
is resolved in one pre-phase (`SharedAncestorResolver.ResolveAllConfigured`),
each honouring the call's insert mode:

1. **Collect** — walk each configured ancestor's Master Template (recursively,
   into nested shared ancestors it references), dependency-ordered. Cycle →
   throw; recursion terminates because ancestors are interned and each is
   visited once.
2. **Generate in memory** — each collected ancestor's Provider runs with
   insert mode forced to `Never`.
3. **Resolve** — one pass per ancestor **sub-graph** (not one pass across all
   of them): a flat ancestor (Provider has no relationships) resolves as a
   single record; a deep one resolves its whole sub-graph depth-batched. This
   is a **known, documented limit**: several *independent* heavy shared
   ancestors each cost their own pass; a converging chain (several ancestors
   sharing a root) is already one pass, since collection dedupes by name.
4. **Main build wires the pre-resolved record** — `SharedRelationshipWiring`
   points every referencing field at the one resolved instance; no per-child
   generation happens for a shared slot.

`SharedAncestor.ResolveNow(lookup, mode)` covers reading `GetId(...)` before
any `Supply*()` call — it triggers the same collect/generate/resolve sequence
for just that ancestor (and its chain), ahead of schedule.

---

## Developer control over resolution

- **`Disable(name)`** — never resolved; every reference leaves the child's FK
  `null`.
- **`ManualResolutionOnly()`** — turns off the pre-phase globally. A
  lightweight ancestor (no sub-graph of its own, auto-detected) still
  resolves on-demand when first referenced; a heavy one throws unless resolved
  up front. **In this port this is a single process-lifetime flag with no
  unsetter** — a real hazard in a shared test process, not just in principle;
  see [reference/known-issues](../reference/known-issues.md) for why this
  port's own test suite cannot safely exercise it.
- **`ResolveNow(lookup, mode, names)`** — resolve a named subset up front, one
  pass, instead of everything a lookup's packaged defaults would otherwise
  pull in.

---

## Interface shape

`ISharedRelationship` (extends `IDefaultRelationship`) adds `SharedName`,
`IsResolved`, `IsResolvedRecordPersisted`, `ResolveSharedRecord(context)`,
and `GetResolvedBundle()`. The factory branches on this interface: a shared
relationship contributes 0 records to generate — just wire the one resolved
instance.

`SharedAncestorProvider` (obtained by chaining onto `SharedAncestor.Put(name,
...)`) carries the shared record's own per-record configuration — value
expressions, its own relationships, `IncludeOptional`, path values,
inclusivity — the same API a generated parent takes, minus anything that only
makes sense for *many* records (no quantity, no template list, no
`SetInsertMode` — persistence follows the referencing call or `ResolveNow`).

---

## Bundle contract

`bundle.GetList(field)` stays aligned 1:1 with the primary records (every
entry the same shared instance). `bundle.GetBundle(field)` exposes the
ancestor's sub-graph once — `SharedAncestor.GetResolvedBundle()` builds a
single-record sub-bundle so the two stay consistent even for a shared
ancestor supplied via `SharedAncestor.PutAsValue(name, record)` rather than
generated.

---

## Known limits (documented, not fixed)

- **One resolution pass per shared-ancestor sub-graph, not one across all of
  them.** Several independent heavy shared ancestors each cost their own
  pass; a converging chain is already one pass.
- **One insert mode per shared ancestor for its lifetime.** Resolving one
  `Mock` and then referencing it from a call using a different mode throws a
  clear "consistent insert mode" error rather than silently drifting.
- **`ManualResolutionOnly()` has no unsetter** — see above.

## Not ported

The Apex design record's acceptance scenario (a 10-level Salesforce Record
Type hierarchy converging on a singleton root) depended entirely on
Salesforce Record Types, which have no C# analog and are not ported (see
[reference/known-issues](../reference/known-issues.md)). The underlying
mechanism it exercised — deep chains converging on one shared root — is still
proven, just against `FlavouredLookupKey`-based variants instead, in
`SharedAncestorHierarchyTest`.
