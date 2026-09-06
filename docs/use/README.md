# Using XFTY

You are here to **write tests for your own code** with XFTY generating the data.

New? Read [getting-started](getting-started.md) top to bottom, then come back to
the per-feature pages as you need them. Each feature page opens with the
simplest example and builds up.

> `InsertMode.Now` inserts for real through a configured
> `IPersistenceGateway`, and throws without one (see
> [insert-modes](insert-modes.md)). Every example on these pages uses `Mock`
> unless the page says otherwise — the practical default for a unit test
> that doesn't need a real database.

---

## Reading order

1. [getting-started](getting-started.md) — the guided tour
2. [generating-records](generating-records.md) — `Supply` / `SupplyList` / `SupplyBundle`, quantity, shorthand constructors
3. [override-templates](override-templates.md) — set only the fields your test cares about; precedence; removal
4. [value-expressions](value-expressions.md) — change *how* a value is generated
5. [relationships](relationships.md) — required / optional, inclusivity, cascading
6. [bundles](bundles.md) — read the generated object graph
7. [insert-modes](insert-modes.md) — `Mock` vs the rest, and what `Now` means today

Then, as needed:

- [context-aware-values](context-aware-values.md) — a field derived from a sibling, an ancestor, or (under `Deferred`) a child
- [per-call-relationships](per-call-relationships.md) — one-off `IncludeOptional` / `ExcludeRelationship` for a single call
- [child-records](child-records.md) — `With` / `WithChildren`: generate the records *below* a primary
- [enrichment](enrichment.md) — `Inject` / `InjectAll`: put parents, child collections and forced fields onto the record for the code under test
- [record-injector](record-injector.md) — `RecordInjector`: the same graft on a plain `List<object>`, no bundle
- [shared-ancestors](shared-ancestors.md) — many children under one parent (flat or deep, auto-detected)
- [deferred-insert](deferred-insert.md) — `Deferred` + `.DepthBatched()`
- [provider-variants](provider-variants.md) — pick a flavour variant
- [advanced/](advanced/) — combining features

Optional add-on packages, each independent of the others:

- [autofixture](autofixture.md) — pair with AutoFixture, both directions
- [autobogus](autobogus.md) — pair with AutoBogus, both directions
- [bogus](bogus.md) — realistic names/emails/addresses/paragraphs via Bogus
- [vector-databases](vector-databases.md) — a random-vector value expression for an embedding field

Two pages describe features deliberately not provided —
[org-seeding](org-seeding.md) and [test-user-helpers](test-user-helpers.md) —
kept as short stubs explaining why, rather than removed outright.

---

## Feature matrix

Every consumer-facing capability and its page.

| Feature | Page |
|---------|------|
| `Supply` / `SupplyList` / `SupplyBundle` | [generating-records](generating-records.md) |
| `SetQuantityPerTemplate`, `SetOverrideTemplateList` | [generating-records](generating-records.md) |
| shorthand constructors (template / list / key) | [generating-records](generating-records.md) |
| `SetOverrideTemplate`, precedence | [override-templates](override-templates.md) |
| `RemoveFromMasterTemplate` | [override-templates](override-templates.md) |
| `Put(field, expression)`, implicit literal | [value-expressions](value-expressions.md) |
| the 7 bundled `*Expression` classes | [value-expressions](value-expressions.md) |
| `CopyFromSiblingExpression` / `CopyFromAncestorExpression` | [context-aware-values](context-aware-values.md) |
| `CopyFromDescendantExpression` — up-flow, `Deferred` only | [context-aware-values](context-aware-values.md) |
| custom `IContextAwareExpression` + `context.SiblingValue` | [context-aware-values](context-aware-values.md) |
| `PutRequired` / `PutOptional`, `SetInclusivity` | [relationships](relationships.md) |
| `PreventCascade`, self-referential cycle guard | [relationships](relationships.md) |
| `IncludeOptional(field)` / `IncludeOptional(path)` / `ExcludeRelationship` | [per-call-relationships](per-call-relationships.md) |
| `With` / `WithChildren` / `ChildProvider` (downward) | [child-records](child-records.md) |
| `Put(List<PropertyInfo>, value)` — path-scoped ancestor values (literal / expression / context-aware / relationship) | [value-expressions](value-expressions.md#setting-a-value-on-a-generated-ancestor) |
| `SharedAncestor` — `Get` / `Put` / `PutAsTemplate` / `PutIfAbsent` / `GetId` | [shared-ancestors](shared-ancestors.md) |
| `SharedAncestor` — deep chains, batched pre-phase, `ResolveNow` | [shared-ancestors](shared-ancestors.md) |
| `bundle.GetList` / `GetBundle` / navigation | [bundles](bundles.md) |
| `bundle.Inject(field, config)` / `InjectAll*`, `InjectConfig` | [enrichment](enrichment.md) |
| `RecordInjector` — standalone graft (parents, children, values) | [record-injector](record-injector.md) |
| insert modes `Never` / `Mock` / `Now` / `Later` / `Deferred`, plus the orthogonal `.ExcludePrimaryIds()` / `.IncludePrimaryIds()` | [insert-modes](insert-modes.md) |
| `Deferred` registry, `.DepthBatched()` | [deferred-insert](deferred-insert.md) |
| `WithVariant` / lookup-key ctor (flavour keys) | [provider-variants](provider-variants.md) |

See [extend/](../extend/README.md) to teach XFTY about a new record type, and
[reference/known-issues.md](../reference/known-issues.md) for the full list of
capability gaps and deliberately out-of-scope features (record-type schema
auto-detection, org seeding, test-user helpers, and more).
