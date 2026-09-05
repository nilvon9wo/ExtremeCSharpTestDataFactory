# Changelog

All notable changes to **this C# port** are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); this project aims to
follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Everything below the **1.0.0-beta.1** entry is inherited, unmodified, from
the Apex original's own changelog (a different codebase, in
[`ExtremeApexTestDataFactory`](https://github.com/nilvon9wo/ExtremeApexTestDataFactory)) -
kept for reference since this port's design faithfully follows it, not
because those entries describe a change made in *this* repository.

## [Unreleased]

### Added

- **`Xfty.Bogus`** — bundled `IValueExpression`s (`FakeFullNameExpression`,
  `FakeEmailAddressExpression`, `FakeStreetAddressExpression`,
  `FakeParagraphExpression`) producing realistic-looking values by wrapping
  Bogus, closing the "no realistic fake data" gap noted in
  [reference/comparison.md](docs/reference/comparison.md) - as a separate,
  opt-in package, so core `Xfty` gains no new dependency.
- **`Xfty.VectorDatabases`** — `RandomVectorExpression(int dimensions, float
  min, float max)`, filling a vector-database record's embedding field with
  a fixed-length array of random floats. Structurally a vector, not a
  semantically meaningful embedding - see
  [roadmap/vector-databases.md](docs/roadmap/vector-databases.md).
- NuGet packaging metadata (`PackageId`, `Version`, `Authors`,
  `PackageLicenseExpression`, embedded `README.md`, symbol packages) for
  every package in the solution; `dotnet pack` verified to produce a valid
  `.nupkg`/`.snupkg` pair for each. Publishing to nuget.org itself remains
  the maintainer's own manual step - see
  [contribute/packaging.md](docs/contribute/packaging.md).

## [1.0.0-beta.1] – 2026-09-05

First beta of the C# port. Feature-complete against the Apex 4.0 surface
this port targets, plus one capability the original never had: real
persistence, via a storage-agnostic seam rather than a Salesforce-specific
one. **Not yet tested against anything approaching a real production
workload** - hence beta.

### Added

- The whole generation engine, ported mechanically: `RecordProvider` /
  `MasterTemplate`, relationship generation (required/optional, per-call
  `IncludeOptional`/`ExcludeRelationship`, `PreventCascade`, ancestor-cycle
  guards), downward generation (`With`/`WithChildren`/`ChildProvider`),
  context-aware values (sibling/ancestor/descendant reads, with the
  mis-ordering guard), shared ancestors, multi-variant Providers
  (`FlavouredLookupKey`), predicates, and bundle enrichment
  (`Inject`/`InjectAll`/`RecordInjector`).
- **`IPersistenceGateway`** — the one-method seam (`Insert(records, idField)`)
  that makes `InsertMode.Now`, `.DepthBatched()`, and
  `DeferredInserter.Flush(gateway)` actually persist, independent of storage
  technology. Proven with an `NSubstitute`-mocked gateway
  (`PersistenceGatewayTest`) and, in the new **`Xfty.EntityFrameworkCore`**
  project, a real `EfPersistenceGateway` proven against SQLite and a real
  Postgres container (via Testcontainers, skipping gracefully without
  Docker).
- **`DiscriminatorLookupKey`** — matching a Provider by a field's value (e.g.
  `Account.Type == "Person"`) on top of `FlavouredLookupKey`, this port's
  analog of a record-type discriminator.
- **Lambda-based field access** throughout the public API -
  `Field.Of<TRecord>(x => x.Field)` and matching overloads on
  `RecordProvider`, `Bundle`, `MasterTemplate<TRecord>`, `ChildProvider`,
  `SharedAncestorProvider`, `FieldPredicateFactory`, and the `CopyFrom*`
  value expressions - so a field is named without a bare `PropertyInfo` or
  `nameof(...)` at the call site.
- **`MasterTemplate<TRecord>`** and **`SimpleRecordProvider<TRecord>`** -
  ergonomic, strongly-typed wrappers (collection-initializer syntax for a
  template; a Provider that is nothing but a template needs no boilerplate)
  over the untyped engine underneath.
- `scripts/verify-doc-examples.py` / `verify-doc-links.py`, wired into CI -
  every documented C# example is exercised by a real test, and every
  relative doc link resolves.

### Changed

- Ported onto idiomatic C#, not a literal syntax translation: reflection
  (`PropertyInfo`/`object`) replaces `SObject`/`SObjectField`; xUnit AAA
  tests replace Apex `@IsTest`; `this.`-qualified members, one expression per
  line, and no inner classes (`file sealed class` instead) throughout.
- Consumers no longer need to know this project's Salesforce origin: every
  Salesforce/Apex/SObject-specific identifier is renamed to a neutral
  equivalent (`RecordInjector`, `RecordType`, `AllowDeeperGraph()`, …), and
  the original Apex source tree (`force-app/`) is no longer carried in this
  repository.
- `xUnit v3` / `Microsoft.Testing.Platform` (from `xunit.runner.visualstudio`
  + VSTest); `dotnet test` opts back in via `global.json`.

### Not ported — deliberate scope boundaries

- Record-type schema auto-detection (no equivalent metadata outside a
  Salesforce org) - covered instead by `DiscriminatorLookupKey`.
- Seeding a long-lived, shared environment (a scratch org, a seeded staging
  database) - a different job from this library's, deliberately not built.
- Test-user helpers and CPU-time/row-count budget tracking - no equivalent
  schema or fixed resource quota to build them against.

See [docs/reference/known-issues.md](docs/reference/known-issues.md) for the
full, current list.

---

## Inherited from the Apex original (reference only - see note above)

## [4.0.0-beta.1] – 2026-09-01

The first public beta of XFTY 4.0. Feature-complete on the `4.0-beta` branch;
APIs may still shift before 4.0 final. See
[docs/reference/migration.md](docs/reference/migration.md) for the upgrade path
from 3.5.

### Added

- **Context-aware values** — a field derived from another record in the graph:
  `XFTY_CopyFromSiblingExpression`, `XFTY_CopyFromAncestorExpression` (single- or
  multi-hop), a custom `XFTY_ContextAwareExpressionIntf`, and `context.siblingValue(field)`
  (a guarded sibling read that throws on a mis-ordered `put` instead of returning
  a misleading `null`).
- **Descendant (up-flowing) value reads** — `XFTY_CopyFromDescendantExpression`
  copies a value up from a generated child, resolved during the `DEFERRED` flush.
- **Shared ancestors** — `XFTY_SharedAncestor`: many children under one generated
  parent, flat or deep (auto-detected), nested, with cycle and depth guards.
  `put` / `putAsTemplate` / `putAsValue` / `putIfAbsent` / `getId`, per-record
  shaping via `XFTY_SharedAncestorProvider`, packaged defaults via
  `XFTY_SharedAncestorDefaultsIntf`, and `disable` / `manualResolutionOnly` /
  `resolveNow` for controlling what gets built.
- **Downward generation** — `with(...)` / `withChildren(...)` / `withChild(...)`
  and `XFTY_SObjectChildProvider` generate the records *below* a primary, nested
  to any depth, `DEFERRED`-aware.
- **Per-call relationship control** — `includeOptional(field)`,
  `includeOptional(path)`, and `excludeRelationship(field)` override inclusivity
  for one call, on the Provider instance.
- **Path-scoped value overrides** — `put(List<SObjectField>, …)` sets how a field
  on a generated ancestor is produced, for one call, without editing that
  ancestor's Provider.
- **Deferred & depth-batched insert** — the `DEFERRED` insert mode plus
  `XFTY_DeferredInserter.flush()` generate across many calls and insert once;
  `.depthBatched()` collapses a `NOW` call to one `insert` per dependency depth.
- **Multi-variant Providers** — record-type and "flavour" lookup keys
  (`XFTY_RecordTypeLookupKey`, `XFTY_FlavouredLookupKey`, `XFTY_FieldPredicate`),
  `withVariant(key)`, and a lookup-key constructor.
- **Governor-limit warnings** — `XFTY_GovernorBudget` writes a `WARN` to the
  debug log when generation alone crosses half of any per-transaction limit;
  tunable through the `XFTY_Settings__c` hierarchy custom setting.
- **Implicit literal values** — `put(field, 'literal')` wraps the value in
  `XFTY_LiteralExpression` for you.
- **Split test suites** — `XFTY_Unit`, `XFTY_Integration`, `XFTY_Load`,
  `XFTY_Examples`, `XFTY_OrgOnly`, and `XFTY_PersonAccount`.
- **`scripts/verify-doc-examples.py`** — CI job that fails the build if a
  documented `apex` example is not backed, line for line, by a runnable test.

### Changed

- **Source format.** XFTY is now a Salesforce DX source-format project
  (`force-app/main/default/classes/<area>/`), with a second, non-default
  `test-support/` package directory for examples and org-only tests.
- **Relationship strategy classes merged.**
  `XFTY_DummyDefaultRelationshipRequired` and `…Optional` are now the single
  `XFTY_DummyDefaultRelationship`; requiredness comes from `putRequired` /
  `putOptional`. Untyped `put(field, <relationship>)` now throws.
- **Provider Lookups replace the global registry.** Every
  `XFTY_DummySObjectProvider` takes a lookup as its second constructor argument.
  `XFTY_DummySObjectProviderLookupIntf` gained `get(XFTY_LookupKeyIntf)` and
  `keysFor(SObject)`. Build one with `XFTY_ProviderLookups.of(map)` or by copying
  `XFTY_DefaultSObjectProviderLookup`.
- **`createBundle` takes an `XFTY_GenerationContext`** instead of three scalar
  arguments. Every custom Provider must update the signature (a one-line change).
- **Value strategies renamed to value expressions** — the `DummyDefault` prefix
  is gone, an `Expression` suffix is added (e.g. `XFTY_DummyDefaultValueIntf` →
  `XFTY_ValueExpressionIntf`, `XFTY_DummyDefaultValueExact` →
  `XFTY_LiteralExpression`). Full table in the migration guide. Behaviour is
  unchanged.
- **`profileIdFor` / `roleIdFor` throw** `UnknownReferenceException` on a miss
  instead of returning `null`.
- **`XFTY_DefaultSObjectProviderLookup.get()` throws** on an unknown `SObjectType`
  instead of swallowing the error.
- Provider-level `put(...)` and `removeFromMasterTemplate(...)`, previously silent
  no-ops, now take effect.

### Removed

- `XFTY_InsertMocker` — was a byte-for-byte duplicate of `XFTY_IdMocker`.
- `IndeterminateSObjectTypeException` and its guards — proven unreachable.
- `XFTY_DummySObjectFactory.cloneAndCompleteNonRelationshipValues` (public
  wrapper) — the logic moved to `XFTY_PlainValueFiller`.

### Fixed

- `XFTY_DummySObjectMasterTemplate` was shallow-cloned between calls.
- `XFTY_RecordTypeDataProvider` re-queried record types on every miss.
- A mismatched override-template list silently retargeted the Provider to a
  different `SObjectType`; it now throws.
- `ALL` inclusivity plus a self-referential relationship recursed until the stack
  overflowed; the ancestor-cycle guard now throws a clear error, and
  `.allowAncestorCycles()` opts out for a chain that terminates on its own.
- A mis-ordered context-aware sibling read returned a silent `null`.
- Real-org compile issues surfaced during beta verification: `@IsTest` on an
  interface, over-length identifiers, a static-initialiser ordering dependency,
  and a field/enum name collision in `XFTY_PathValue`.

### Coverage

- 100% line coverage, verified on a scratch org (the framework ships as
  `@IsTest`, so Salesforce reports 0% until the annotation is stripped for
  measurement). Every one of the ~424 tests passes; zero classes carry an
  uncovered line.

## [3.5.0] – prior to 4.0 development

Baseline. Single-argument Providers, a global Provider registry, relationship
strategy classes split by requiredness, "value strategy" naming, and the pre-DX
`src/` layout. Tagged retroactively so the 4.0 migration guide and release notes
have a fixed reference point.

[4.0.0-beta.1]: https://github.com/nilvon9wo/ExtremeApexTestDataFactory/tree/4.0-beta
[3.5.0]: https://github.com/nilvon9wo/ExtremeApexTestDataFactory/releases/tag/v3.5.0
