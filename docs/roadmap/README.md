# XFTY Roadmap

What is built, what is left, for this C# port.

Legend: ✅ built and working · ⚠️ built but with a real limitation versus the
Apex original · ❌ not ported (see [reference/known-issues](../reference/known-issues.md) for why) · 💡 idea, not designed · 🧪 preview proof-of-concept - works, but not a considered, general-purpose package yet · 🚫 considered and declined - a deliberate non-goal, not a gap.

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
| **Descendant (up-flowing) value reads** — `CopyFromDescendantExpression`, multi-hop via its path-list constructor | `CopyFromDescendantExpressionTest` | [use](../use/context-aware-values.md#reading-up-from-a-child), [detail](descendant-value-reads.md) | ⚠️ First matching child at every hop - no way to read an aggregate across siblings (a custom `IDeferredExpression` using `DeferredGraph.ChildIndicesOf` directly can, if needed). |
| **Shared ancestors** — `SharedAncestor.Put/Get`, flat + deep auto-detected, nested, cycle guards, `SharedAncestorProvider` per-record config, `ISharedAncestorDefaults` packaged defaults, `Disable` / `ManualResolutionOnly` / batch `ResolveNow` / `ResetAllForTesting` | `SharedAncestorTest`, `SharedAncestorHierarchyTest`, `SharedAncestorResetTest`, `SharedAncestorConcurrencyTest` | [use](../use/shared-ancestors.md), [extend](../extend/shared-ancestors-in-templates.md), [detail](shared-ancestors.md) | ⚠️ Static registry does **not** reset between xUnit test methods automatically, unlike Apex — three ways to handle it (`[IsolatesSharedAncestor]`, `ResetAllForTesting()`, or unique names), none automatic; see [reference/salesforce-considerations](../reference/salesforce-considerations.md). Safe under *concurrent* access regardless of which you pick - was a real, reproduced crash risk, now fixed (`ConcurrentDictionary` + a resolver-level lock). |
| **`[IsolatesSharedAncestor]`** — resets `SharedAncestor` before/after a test class or method, via xUnit's `BeforeAfterTestAttribute` | `IsolatesSharedAncestorAttributeTest`, `SharedAncestorLeaksWithoutIsolationTest` | [use/shared-ancestors](../use/shared-ancestors.md) | ✅ Separate opt-in package (`Xfty.Xunit`) - the same reset as `ResetAllForTesting()`, wired up automatically instead of by hand. |
| **Enrichment** — `bundle.Inject(field, config)` / `InjectAll` / `InjectAllParents` / `InjectAllChildren`, `InjectConfig`, standalone `RecordInjector` | `BundleEnricherTest`, `RecordInjectorTest`, `EnrichmentSelectionTest`, `EnrichmentIntegrationTest` | [use](../use/enrichment.md), [injector](../use/record-injector.md) | ✅ — reflection sets any property directly, so there's no serialization round-trip and no field-type special-casing needed |
| **Predicates** — `FieldPredicateFactory`, `PredicateFactory` (AND/OR/NOT), custom `IRecordPredicate` | `Xfty.Test/Predicates/*` | [extend/provider-variants](../extend/provider-variants.md) | ✅ |
| **Realistic fake data** — `FakeFullNameExpression`, `FakeEmailAddressExpression`, `FakeStreetAddressExpression`, `FakeParagraphExpression`, wrapping Bogus | `Xfty.Bogus.Test/*` | [comparison.md](../reference/comparison.md#could-xfty-pair-with-one-of-these-to-close-a-gap) | ✅ Separate opt-in package (`Xfty.Bogus`), not core `Xfty` — the base library has no dependency on Bogus. |
| **Vector-embedding fields** — `RandomVectorExpression(int dimensions, float min, float max, bool normalize)`, `KnownEmbeddingDimensions` | `Xfty.VectorDatabases.Test/*` | [vector-databases.md](vector-databases.md) | ✅ Separate opt-in package (`Xfty.VectorDatabases`); structurally a vector, not a semantically meaningful embedding — see the detail page for why that's out of scope. |
| **pgvector persistence** — a `Vector`-typed column through the *existing, unmodified* `EfPersistenceGateway` | `PgVectorPersistenceTest` (`Xfty.EntityFrameworkCore.Test`) | [vector-databases.md](vector-databases.md#pgvector-through-the-existing-efpersistencegateway---proven-no-new-gateway-code) | ✅ No new gateway code - just a `Pgvector.EntityFrameworkCore` reference, a demo entity, and a `pgvector/pgvector:pg16` container image instead of plain `postgres:16-alpine`. |
| **AutoFixture pairing** — `XftyCustomization`/`XftySpecimenBuilder` (point `fixture.Create<T>()` at a registered `RecordProvider`), `IUnsetFieldFiller`/`AutoFixtureUnsetFieldFiller` (let AutoFixture fill fields a Master Template never configured) | `Xfty.AutoFixture.Test/*`, `UnsetFieldFillerTest` (core contract) | [use](../use/autofixture.md), [detail](autofixture-fallback-fill.md) | ✅ Separate opt-in package (`Xfty.AutoFixture`); core `Xfty` gains only the dependency-free `IUnsetFieldFiller` extension point, not a dependency on AutoFixture itself. |
| **Typed `RecordProvider<TRecord>`/`ChildProvider<TChild>`** — composed generic wrappers mirroring `MasterTemplate<TRecord>`, plus `LookupKey.Get<TRecord>()`/`FlavouredLookupKey.Get<TRecord>(flavour)`/`lookup.Get<TRecord>()` | `RecordProviderOfTTest`, `ChildProviderOfTTest`, `LookupKeyTest` | — | ✅ Typed `Supply()`/`SupplyList()` (no cast), a `MasterTemplate<TRecord>`-style object-initializer indexer, and full fluent forwarding. |

## Not ported — genuine capability gaps

| Feature | Why |
|---------|-----|
| Seeding a long-lived, shared environment | A different job from generating/inserting data for one test run - deliberately out of scope. [sandbox-seeding.md](sandbox-seeding.md). |
| Record-type schema auto-detection | Inferring a variant from an override template's own discriminator-shaped metadata needs schema description this port has no equivalent of. `DiscriminatorLookupKey` covers the actual use case (matching by a named field's value). |
| Test-user helpers (an admin-equivalent user, role/profile lookups) | No role/profile-style schema for such a lookup to resolve against. |
| CPU-time/row-count budget tracking | No fixed per-run resource quota exists to track against — see [reference/volume-and-limits](../reference/volume-and-limits.md) for what replaces it. |
| Namespace / package distribution | Salesforce-specific distribution concept — [namespace-appexchange.md](namespace-appexchange.md). |

---

## Ideas under consideration — not gaps, not committed to

| Idea | Status | Detail |
|------|--------|--------|
| Embedded/denormalized document relationships (a document database's native nested-array shape, distinct from the FK-reference relationships XFTY models today) | 💡 | [embedded-documents.md](embedded-documents.md) |

---

## Preview proof-of-concept packages — work, but not a general-availability commitment yet

Versioned `0.x-preview.*`, not `1.0.0-beta.1` like the rest of this
solution's packages, on purpose. Read the package's own README before
relying on one of these for anything beyond the question it was built to
answer.

| Package | Question it answers | Detail |
|---------|---------------------|--------|
| `Xfty.VectorDatabases.Qdrant` — `QdrantPersistenceGateway`, via Qdrant's own client directly | Is a dedicated vector-database `IPersistenceGateway` a trivial wrapper or real design work, without going through an abstraction layer? (Answer: real work, but less than the MEVD path - compiled and passed on the first attempt.) | [vector-databases.md](vector-databases.md#qdrant---two-separate-preview-packages-not-a-considered-release), [package README](../../Xfty.VectorDatabases.Qdrant/README.md) |
| `Xfty.VectorDatabases.MicrosoftExtensionsVectorData` — `MevdPersistenceGateway`, via any Microsoft.Extensions.VectorData connector | Is the extra abstraction worth it - and can one gateway really work with any vector store, not just the one it's tested against? (Answer: the abstraction didn't pay for itself on this simple a record shape, but the genericity claim holds by construction - `GetDynamicCollection` etc. are on MEVD's abstract base class, not Qdrant-specific.) Kept in a separate package from the Qdrant-direct gateway from day one - see the detail page for why. | [vector-databases.md](vector-databases.md#qdrant---two-separate-preview-packages-not-a-considered-release), [package README](../../Xfty.VectorDatabases.MicrosoftExtensionsVectorData/README.md) |

---

## Explicitly declined — deliberate non-goals

Considered and turned down on purpose, not gaps waiting to be filled.

| Idea | Why declined | Detail |
|------|--------------|--------|
| Calling a real embedding API (OpenAI, Cohere, …) to produce a semantically meaningful vector | Breaks XFTY's offline/no-network/no-credential contract that every other value expression, `Xfty.Bogus` included, holds to. A project that needs real embeddings is better served by its own small helper than by XFTY adopting a paid-API pattern it uses nowhere else. | [vector-databases.md](vector-databases.md#deliberately-out-of-scope-calling-a-real-embedding-model) |

---

## Decided, not yet done

| Item | Status | Detail |
|------|--------|--------|
| Publish `Xfty`, `Xfty.EntityFrameworkCore`, `Xfty.Bogus`, `Xfty.VectorDatabases`, `Xfty.Xunit`, and `Xfty.AutoFixture` to nuget.org | All six packages build/pack cleanly and are verified locally; the actual push needs the maintainer's own nuget.org account and API key. | [contribute/packaging.md](../contribute/packaging.md) |

---

## Standing constraints (facts, not tasks)

- **Branch coverage cannot be measured by tooling** — hand-checked on every
  change. [coverage-standards.md](../contribute/coverage-standards.md).
- **Static state does not reset between xUnit test methods**, the opposite of
  Apex's per-test-method reset. Documented, and mitigated by a naming/cleanup
  convention rather than "fixed" (it is inherent to how .NET statics work).
  [salesforce-considerations.md](../reference/salesforce-considerations.md).
