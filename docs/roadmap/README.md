# XFTY Roadmap

What is built, what is left, for this C# port.

Legend: ✅ built and working · ⚠️ built but with a real limitation versus the
Apex original · ❌ not ported (see [reference/known-issues](../reference/known-issues.md) for why) · 📋 designed, not built.

## Built

| Feature | Tests | Docs | Notes / limits |
|---------|-------|------|----------------|
| **Multi-variant Providers** — flavour lookup keys, `WithVariant`, the lookup-key constructor, `DiscriminatorLookupKey` (discriminator-field variants) | `LookupKeyTest`, `MultiVariantProviderTest`, `VariantResolutionTest`, `DiscriminatorLookupKeyTest` | [use](../use/provider-variants.md), [extend](../extend/provider-variants.md), [detail](multi-variant-providers.md) | ✅ |
| **Context-aware values** — `CopyFromSiblingExpression`, `CopyFromAncestorExpression` (multi-hop), custom `IContextAwareExpression`, `context.SiblingValue` | `RecordFactoryTest`, `Xfty.Test/Values/*`, `ContextAwareExpressionTest` | [use](../use/context-aware-values.md), [extend](../extend/custom-value-expressions.md), [detail](context-aware-values.md) | ✅ |
| **Loud guard for a mis-ordered sibling read** | `ContextAwareExpressionTest`, `CopyFromSiblingExpressionTest` | [use](../use/context-aware-values.md) | ✅ |
| **Per-call relationship control** — `IncludeOptional(field \| path)`, `ExcludeRelationship` | `RecordFactoryTest`, `AncestorCycleTest` | [use](../use/per-call-relationships.md) | ✅ |
| **Path-scoped value overrides** — `Put(List<PropertyInfo>, …)` into a generated ancestor | `PathValueTest` | [use](../use/value-expressions.md#setting-a-value-on-a-generated-ancestor), [detail](path-scoped-values.md) | ✅ |
| **Downward generation** — `With` / `WithChildren` / `ChildProvider`, nested grandchildren | `ChildProviderTest`, `PerformanceTest` (3-level fan-out) | [use](../use/child-records.md) | ✅ |
| **Deferred / depth-batched resolution** — `Deferred` + registry, `.DepthBatched()` | `DeferredInserterTest`, `DeferredInsertBufferTest`, `DepthBatchedInserterTest`, `PersistenceGatewayTest` | [use](../use/deferred-insert.md), [detail](deferred-persistence.md) | ✅ `Flush(gateway)` and `Now`+`.DepthBatched()` both insert for real through a configured `IPersistenceGateway`; proven against SQLite and a real Postgres container in `Xfty.EntityFrameworkCore.Test`. |
| **Descendant (up-flowing) value reads** — `CopyFromDescendantExpression` | `CopyFromDescendantExpressionTest` | [use](../use/context-aware-values.md#reading-up-from-a-child), [detail](descendant-value-reads.md) | ⚠️ First matching child, single hop only. |
| **Shared ancestors** — `SharedAncestor.Put/Get`, flat + deep auto-detected, nested, cycle guards, `SharedAncestorProvider` per-record config, `ISharedAncestorDefaults` packaged defaults, `Disable` / `ManualResolutionOnly` / batch `ResolveNow` | `SharedAncestorTest`, `SharedAncestorHierarchyTest` | [use](../use/shared-ancestors.md), [extend](../extend/shared-ancestors-in-templates.md), [detail](shared-ancestors.md) | ⚠️ Static registry does **not** reset between xUnit test methods, unlike Apex — see [reference/salesforce-considerations](../reference/salesforce-considerations.md). `ManualResolutionOnly()` cannot be safely tested in this port's own shared-process suite for the same reason. |
| **Enrichment** — `bundle.Inject(field, config)` / `InjectAll` / `InjectAllParents` / `InjectAllChildren`, `InjectConfig`, standalone `RecordInjector` | `BundleEnricherTest`, `RecordInjectorTest`, `EnrichmentSelectionTest`, `EnrichmentIntegrationTest` | [use](../use/enrichment.md), [injector](../use/record-injector.md) | ✅ — reflection sets any property directly, so there's no serialization round-trip and no field-type special-casing needed |
| **Predicates** — `FieldPredicateFactory`, `PredicateFactory` (AND/OR/NOT), custom `IRecordPredicate` | `Xfty.Test/Predicates/*` | [extend/provider-variants](../extend/provider-variants.md) | ✅ |

## Not ported — genuine capability gaps

| Feature | Why |
|---------|-----|
| Seeding a long-lived, shared environment | A different job from generating/inserting data for one test run - deliberately out of scope. [sandbox-seeding.md](sandbox-seeding.md). |
| Record-type schema auto-detection | Inferring a variant from an override template's own discriminator-shaped metadata needs schema description this port has no equivalent of. `DiscriminatorLookupKey` covers the actual use case (matching by a named field's value). |
| Test-user helpers (an admin-equivalent user, role/profile lookups) | No role/profile-style schema for such a lookup to resolve against. |
| CPU-time/row-count budget tracking | No fixed per-run resource quota exists to track against — see [reference/volume-and-limits](../reference/volume-and-limits.md) for what replaces it. |
| Namespace / package distribution | Salesforce-specific distribution concept — [namespace-appexchange.md](namespace-appexchange.md). |

---

## Open questions

Apex's open question — "does XFTY commit to a deployable, non-`@IsTest`
distribution?" — **does not apply to C#.** There is no `@IsTest`-annotation
concept that keeps compiled code out of a real build; a C# library is always
"real" code. See [open-questions.md](open-questions.md).

The persistence question this page used to track as open is resolved:
`IPersistenceGateway` plus `Xfty.EntityFrameworkCore` unblock `Now`,
`DeferredInserter.Flush(gateway)`, and `.DepthBatched()`. Seeding a
long-lived, shared environment remains a deliberately separate, out-of-scope
concern — see [sandbox-seeding.md](sandbox-seeding.md).

---

## Standing constraints (facts, not tasks)

- **Branch coverage cannot be measured by tooling** — hand-checked on every
  change. [coverage-standards.md](../contribute/coverage-standards.md).
- **Static state does not reset between xUnit test methods**, the opposite of
  Apex's per-test-method reset. Documented, and mitigated by a naming/cleanup
  convention rather than "fixed" (it is inherent to how .NET statics work).
  [salesforce-considerations.md](../reference/salesforce-considerations.md).
