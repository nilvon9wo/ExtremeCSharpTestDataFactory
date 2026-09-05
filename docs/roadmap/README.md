# XFTY Roadmap

What is built, what is left, for this C# port.

Legend: ✅ built and working · ⚠️ built but with a real limitation versus the
Apex original · ❌ not ported (see [reference/known-issues](../reference/known-issues.md) for why) · 📋 designed, not built.

## Built

| Feature | Tests | Docs | Notes / limits |
|---------|-------|------|----------------|
| **Multi-variant Providers** — flavour lookup keys, `WithVariant`, the lookup-key constructor | `LookupKeyTest`, `MultiVariantProviderTest`, `VariantResolutionTest` | [use](../use/provider-variants.md), [extend](../extend/provider-variants.md), [detail](multi-variant-providers.md) | Salesforce Record Type variants (`RecordTypeLookupKey`) not ported — ❌ |
| **Context-aware values** — `CopyFromSiblingExpression`, `CopyFromAncestorExpression` (multi-hop), custom `IContextAwareExpression`, `context.SiblingValue` | `RecordFactoryTest`, `Xfty.Test/Values/*`, `ContextAwareExpressionTest` | [use](../use/context-aware-values.md), [extend](../extend/custom-value-expressions.md), [detail](context-aware-values.md) | ✅ |
| **Loud guard for a mis-ordered sibling read** | `ContextAwareExpressionTest`, `CopyFromSiblingExpressionTest` | [use](../use/context-aware-values.md) | ✅ |
| **Per-call relationship control** — `IncludeOptional(field \| path)`, `ExcludeRelationship` | `RecordFactoryTest`, `AncestorCycleTest` | [use](../use/per-call-relationships.md) | ✅ |
| **Path-scoped value overrides** — `Put(List<PropertyInfo>, …)` into a generated ancestor | `PathValueTest` | [use](../use/value-expressions.md#setting-a-value-on-a-generated-ancestor), [detail](path-scoped-values.md) | ✅ |
| **Downward generation** — `With` / `WithChildren` / `ChildProvider`, nested grandchildren | `ChildProviderTest`, `PerformanceTest` (3-level fan-out) | [use](../use/child-records.md) | ✅ |
| **Deferred / depth-batched resolution** — `Deferred` + registry, `.DepthBatched()` | `DeferredInserterTest`, `DeferredInsertBufferTest`, `DepthBatchedInserterTest` | [use](../use/deferred-insert.md), [detail](deferred-persistence.md) | ⚠️ **No persistence layer** — `Flush()` and `Now`+`.DepthBatched()` always throw. Only the in-memory graph-building/flattening side is usable today. |
| **Descendant (up-flowing) value reads** — `CopyFromDescendantExpression` | `CopyFromDescendantExpressionTest` | [use](../use/context-aware-values.md#reading-up-from-a-child), [detail](descendant-value-reads.md) | ⚠️ Resolvable via `DeferredInsertBuffer.Flatten(bundle)`; a real `Flush()` still throws. First matching child, single hop only. |
| **Shared ancestors** — `SharedAncestor.Put/Get`, flat + deep auto-detected, nested, cycle guards, `SharedAncestorProvider` per-record config, `ISharedAncestorDefaults` packaged defaults, `Disable` / `ManualResolutionOnly` / batch `ResolveNow` | `SharedAncestorTest`, `SharedAncestorHierarchyTest` | [use](../use/shared-ancestors.md), [extend](../extend/shared-ancestors-in-templates.md), [detail](shared-ancestors.md) | ⚠️ Static registry does **not** reset between xUnit test methods, unlike Apex — see [reference/salesforce-considerations](../reference/salesforce-considerations.md). `ManualResolutionOnly()` cannot be safely tested in this port's own shared-process suite for the same reason. |
| **Enrichment** — `bundle.Inject(field, config)` / `InjectAll` / `InjectAllParents` / `InjectAllChildren`, `InjectConfig`, standalone `RecordInjector` | `BundleEnricherTest`, `RecordInjectorTest`, `EnrichmentSelectionTest`, `EnrichmentIntegrationTest` | [use](../use/enrichment.md), [injector](../use/record-injector.md) | ✅ — simpler than Apex (no JSON round-trip, so no Blob/compound/polymorphic special-casing needed) |
| **Predicates** — `FieldPredicateFactory`, `PredicateFactory` (AND/OR/NOT), custom `IRecordPredicate` | `Xfty.Test/Predicates/*` | [extend/provider-variants](../extend/provider-variants.md) | ✅ |

## Not ported — genuine capability gaps

| Feature | Why |
|---------|-----|
| `InsertMode.Now` actually persisting | No persistence layer exists yet. See [reference/known-issues](../reference/known-issues.md). |
| Org data seeding (`XFTY_Seeder`) | No live, persistent environment exists to seed. [sandbox-seeding.md](sandbox-seeding.md). |
| Salesforce Record Type variants | No C# analog for `RecordTypeId` / schema describe. |
| Test-user helpers (`TEST_ADMIN_USER`, `profileIdFor`, `roleIdFor`) | No live org, `Profile`, or `UserRole` concept. |
| Governor-limit warnings (`XFTY_GovernorBudget`) | No C# analog for `Limits.getCpuTime()` etc. — see [reference/volume-and-limits](../reference/volume-and-limits.md) for what replaces it. |
| Namespace / AppExchange packaging | Salesforce-specific distribution concept — [namespace-appexchange.md](namespace-appexchange.md). |

---

## The open question this port actually has

Apex's open question — "does XFTY commit to a deployable, non-`@IsTest`
distribution?" — **does not apply to C#.** There is no `@IsTest`-annotation
concept that keeps compiled code out of a real build; a C# library is always
"real" code. See [open-questions.md](open-questions.md).

This port's actual open question is: **when, and how, does a real persistence
layer (EF Core or similar) get wired up**, since that unblocks `Now`,
`DeferredInserter.Flush()`, `.DepthBatched()`, and (eventually) org/database
seeding. Nothing is designed for this yet — see [csharp-port-idea.md](../../csharp-port-idea.md)
at the repo root for the standing project notes.

---

## Standing constraints (facts, not tasks)

- **Branch coverage cannot be measured by tooling** — hand-checked on every
  change. [coverage-standards.md](../contribute/coverage-standards.md).
- **Static state does not reset between xUnit test methods**, the opposite of
  Apex's per-test-method reset. Documented, and mitigated by a naming/cleanup
  convention rather than "fixed" (it is inherent to how .NET statics work).
  [salesforce-considerations.md](../reference/salesforce-considerations.md).
