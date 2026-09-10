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

### Changed

- **The `docs/use/` guides now lead with `new RecordProvider<Contact>(lookup)`**
  (`Net.NowhereAtAll.Xfty.Core.RecordProviders`) — `Supply()` returns
  `Contact`, `SupplyList()` returns `List<Contact>`, no `(Contact)` cast. The
  typed wrapper always existed; the guides had just never adopted it. The
  non-generic `new RecordProvider(typeof(Contact), lookup)`
  (`Net.NowhereAtAll.Xfty.Core`) and the shorthand constructors are still
  shown where they read better. `getting-started.md`'s `using` block and the
  API cheat-sheet now name both namespaces; the `Ex*Test` doc-mirror tests
  were updated to match.

## [1.0.0-beta.9] – 2026-09-10

> **Theme: XFTY stops assuming your records look like Salesforce's.** A
> non-nullable value-type field now receives its configured value; a primary
> key needn't be a `string` or be named `Id`; `struct` / `record struct` /
> positional records generate; and the placeholder Id under `InsertMode.Mock`
> is a pluggable `IMockIdGenerator` per record type and per Provider variant.
> The Apex→C# translation phase is over — `docs/contribute/porting-history.md`
> ("Development History") records the shift.

### Added

- **Pluggable mock-Id generation — `IMockIdGenerator`.** Under
  `InsertMode.Mock`, the placeholder identifier's shape is now an
  `IMockIdGenerator`. `DefaultMockIdGenerator` renders one process-wide
  sequence as the key field's own type: `"mock-N"` for a `string`, `N` for
  `int`/`long`, a fresh `Guid` for `Guid`; any other Id type throws
  `XftyConfigurationException` naming the type. So `InsertMode.Mock` now
  works for a non-string primary key out of the box, and a project with its
  own Id shape ("A" + counter + timestamp, …) supplies its own generator —
  per record type on the Master Template (`WithMockIdGenerator`), or per call
  on the provider (`RecordProvider.SetMockIdGenerator`, which overrides the
  template's for that call's primary records only; ancestors keep their own).
  Honoured in the depth-batched / deferred insert path too, per record type,
  and picked per *resolved Provider variant* — a discriminated flavour of one
  type can carry its own generator. `IMockIdGenerator` / `MockIdContext` /
  `DefaultMockIdGenerator` in `Xfty/Persistence/`; `MockIdGeneratorTest`,
  `DeepMixedKeyHierarchyTest` (a ten-deep ancestor chain, no two keys named or
  typed alike), `FlavouredMockIdHierarchyTest` (one POCO, eight variants,
  eight generations); docs at `docs/extend/mock-id-generators.md`.

### Fixed

- **The persistence path assumed the primary-key property was named `Id`.**
  `IdMocker`'s mixed-type overload, `DepthBatchedInserter`,
  `PersistenceGatewayExtensions.InsertMixed`, `InverseAlignment`,
  `LookupWiring`'s relationship fallback, `RecordProviderChildConfig`, and
  `SharedAncestor` (`GetId`, the resolved single-record bundle) all reached
  for a property literally called `Id`. A project whose keys are named after
  the table (`LedgerId` on `Ledger`) now works: the key field comes from the
  Provider's `PrimaryTargetField`, carried on every `Bundle` and threaded
  through the depth-batched inserter as an id-field-by-type map. The `"Id"`
  reflection remains only as a last-resort fallback for a hand-built caller
  that supplies neither. `NonIdPrimaryKeyTest`, `DeepMixedKeyHierarchyTest`.
- **`InsertMode.Mock` overwrote a primary key the template had already set.**
  `RecordFactory`'s Mock pass now fills only an unset key (matching the
  depth-batched path, which already did) — `new Order { OrderRef = "known-1" }`
  keeps `"known-1"`.
- **`SharedAncestor.Put(name, record)` regenerated a fully-formed record with
  a non-`Id`-named key** instead of using it as-is: `Put` still checks an
  `Id`-named property (no lookup at registration time), but the resolver now
  makes the value-vs-generate call from the Provider's real key field, so
  `PutAsTemplate(name, recordWithKeySet)` is honoured as a fixed value.
- **`Xfty.VectorDatabases.Qdrant` / `.MicrosoftExtensionsVectorData`
  (preview)** threw an opaque `InvalidOperationException` when a record had
  no `float[]` property, and silently picked one when it had several — now a
  clear `NotSupportedException` either way.
- **A non-nullable value-type field never received its configured value.**
  Every value pass gated filling on `field.GetValue(record) is not null`,
  which is always true for a boxed `0` / `false` / empty `Guid` /
  `default(DateTime)` / a zero enum — so a configured plain default, a
  context-aware value, and a wired foreign key were all silently skipped for
  an `int` / `bool` / `Guid` / `enum` / `DateTime` field. This was never
  `struct`-specific — it bit any such field on a class or `record` too. Fixed
  with `Engine/FieldState.IsUnset`, which compares the current value against
  what a freshly-built instance would hold (`null` for a reference type or
  `Nullable<T>`, the type default for a non-nullable value type); it now backs
  `PlainValueFiller`, `ContextAwareValuePass`, `DescendantValuePass`, and
  `LookupWiring`. The residual (unchanged, and symmetric with reference types,
  where it always existed): an *override template* can't force a field to its
  empty value — `0` / `false` / `default` for a value type, `null` for a
  reference type — when the Provider defaults it, because that's
  indistinguishable from "unset" through reflection. The tracked
  `provider[x => x.Field] = value` form, or
  `RemoveFromMasterTemplate(x => x.Field)`, does it. `NonNullableValueFieldTest`
  and `ForcingAnEmptyFieldValueTest` cover both sides.
- **Positional records now generate.** A positional `record class`
  (`record Contact(string Id, string Name)`) has no parameterless
  constructor, so `Activator.CreateInstance` threw `MissingMethodException`
  the moment XFTY tried to build a blank template or clone one. New
  `Internal/BlankInstances.Of` uses the public parameterless constructor when
  there is one (running field initializers, as before) and an uninitialized
  instance otherwise — every field is set by reflection immediately after
  either way. `RuntimeHelpers.GetUninitializedObject` on net8.0+,
  `FormatterServices.GetUninitializedObject` on netstandard2.0 (exercised by
  the net472 `Xfty.NetStandardCompat.Test`). Applied everywhere a record type
  is instantiated: `RecordCloneFactory`, `RecordProvider`,
  `SharedAncestorProvider`, `ChildProvider`. XFTY makes no assumption about
  how a consumer constructs its own database records. `ValueTypeRecordSupportTest`
  covers `record struct`, `readonly record struct`, positional `record class`,
  and positional `record struct`.

### Changed

- **`docs/reference/known-issues.md` rewritten** as a plain "what you tried /
  what you got / what to do instead" list — no implementation detail, no
  defect backlog. Fixed-bug history and the open doc-verification item moved
  to `docs/contribute/porting-history.md`; the "Apex features with no .NET
  equivalent" list demoted to a short footnote. The variant-inference wording
  was also wrong and is corrected: XFTY *does* match an override template to
  a registered `FlavouredLookupKey`/`DiscriminatorLookupKey` automatically —
  only Apex's zero-registration schema detection is gone.

## [1.0.0-beta.8] – 2026-09-10

> **Changelog catch-up.** `1.0.0-beta.2` through `1.0.0-beta.7` were tagged and
> published without their own changelog sections. The entries below therefore
> cover everything since `1.0.0-beta.1`, not just this release — most of it
> already shipped in those intermediate betas. Removing `SimpleRecordProvider`
> is the one change genuinely new in `beta.8`. Per-release sections (and
> automated publishing — a release now happens when a `## [x.y.z]` heading
> like this one lands on `master`) resume here.

### Added

- **Core `Xfty` now multi-targets `netstandard2.0;net8.0;net10.0`**, not just
  `net10.0` - a `net8.0`/`net9.0`/`net10.0`+ consumer gets an exact or
  compatible match either way, and `netstandard2.0` reaches everything else
  that implements it (.NET Framework 4.6.1+, older .NET Core, Mono/Xamarin)
  without asking any of them to move off it. Verified empirically, not just
  assumed: all three TFMs build clean, and (since `netstandard2.0` is a
  contract, not itself a runnable platform - the actual execution always
  happens on whatever real runtime implements it) a new `net472` project,
  `Xfty.NetStandardCompat.Test`, proves the `netstandard2.0`-only
  compatibility shims genuinely run, not just compile: `Dictionary.GetValueOrDefault`
  and `IEnumerable.ToHashSet` (.NET Core 2.0+) and `Random.Shared` (.NET 6+)
  don't exist on `netstandard2.0`'s real BCL, so `Xfty/Internal/NetStandardCompat.cs`
  supplies each - the same call syntax everywhere else in the codebase, so no
  other file needed to change - with `SharedRandom` specifically using a
  per-thread `Random` instance on `netstandard2.0` (matching `Random.Shared`'s
  actual thread-safety guarantee, not just its name: plain `Random` isn't
  thread-safe on the older runtimes `netstandard2.0` targets). `init`
  accessors (used only in the `Xfty.Demo` model classes) needed
  `IsExternalInit`, supplied by PolySharp rather than a hand-rolled shim.
  `LangVersion` is now pinned explicitly (`latest`, in `Directory.Build.props`)
  for the whole solution - without it, `netstandard2.0`'s compile pass would
  silently default to a much older C# language version and reject `init`,
  primary constructors, collection expressions, etc. outright. CI gained a
  dedicated `windows-net472` job (the only real runtime available that
  implements `netstandard2.0`, and the only one of the three TFMs Linux
  runners can't execute) - `Xfty.ci-cross-platform.slnf` keeps the existing
  Linux job scoped to what it can actually run.
- **`Xfty.FSharpAsync`** - F#-idiomatic `Async<'T>` wrappers
  (`RecordProviderAsync`, `TypedRecordProviderAsync`, `DeferredInserterAsync`)
  over the plain and typed `RecordProvider`'s `Task`-based
  `Supply`/`SupplyList`/`SupplyBundle` and `DeferredInserter.Flush`, for F#
  code built on the original `async { }` workflow rather than the newer
  `task { }` computation expression (which needs no wrapper at all - it
  already consumes `Xfty`'s `Task`-returning members directly). Each function
  is a thin `Async.AwaitTask` bridge - the underlying work is identical
  either way; the reason this exists at all is that `Async<'T>` is cold
  (nothing runs until something starts it) where `Task<'T>` is hot (already
  running, or scheduled, the moment you hold a reference to it), a real
  semantic difference `Async.AwaitTask` alone doesn't erase. See
  [Xfty.FSharpAsync/README.md](Xfty.FSharpAsync/README.md).

### Changed

- **BREAKING: persistence is now fully async, end to end.** `IPersistenceGateway.Insert`
  returns `Task` instead of `void`, and every method in the call chain that can
  reach it now does too - `RecordProvider.Supply()`/`SupplyList()`/`SupplyBundle()`,
  `IRecordProvider.CreateBundle`, `RecordFactory.Build`, `DepthBatchedInserter.ResolveAll`/`InsertAll`,
  `DeferredInsertBuffer`/`DeferredInserter.Flush`, `SharedAncestor`'s resolution
  methods (`ResolveNow`, `PutRequired`'s underlying resolver), and both
  vector-database gateways (`QdrantPersistenceGateway`, `MevdPersistenceGateway`,
  now awaiting the Qdrant/MEVD clients' own async APIs directly instead of
  blocking on them). There was no good reason to keep this synchronous - it was
  inherited from the Apex original, which has no `async`/`await` at all - and
  every real backing store (`DbContext.SaveChangesAsync`, a vector database
  client, a network call) is naturally asynchronous underneath. Two adapters
  that implement third-party *synchronous* SPIs cannot become `async`
  themselves - `Xfty.AutoFixture`'s `XftySpecimenBuilder.Create`
  (`ISpecimenBuilder.Create`) and `Xfty.AutoBogus`'s
  `XftyAutoBogusOverride.Generate` (`AutoGeneratorOverride.Generate`) - and
  bridge with `Task.Run(...).GetAwaiter().GetResult()` instead, documented on
  each. The `Task.Run` wrapper is deliberate, not decoration: it runs XFTY's
  generation on a fresh thread-pool thread with no captured
  `SynchronizationContext`, so the blocking wait can never deadlock waiting
  on a continuation that needed the very thread it's blocking on - safe
  regardless of what thread calls in (a UI thread, a classic ASP.NET request,
  anything), not just the xUnit/CI context these adapters are normally used
  from. XFTY is itself a piece of test infrastructure sitting between the
  code under test and the tests exercising it, so this errs toward
  eliminating a whole class of hard-to-diagnose hangs at a negligible
  thread-pool-hop cost, rather than documenting the risk away for only the
  contexts already known to be safe. None of
  the new async methods carry an `Async` suffix - every one of them is the
  direct (now-`Task`-returning) replacement for the same-named synchronous
  method it replaces, not a parallel overload, so the suffix would be pure
  noise next to the `async`/`await` keywords already marking the call.
  `SharedAncestorResolver`'s old `lock`-based mutual exclusion could not
  contain `await` at all (and its reentrancy tracking relied on OS-thread
  identity, which an async continuation can resume on a different thread from)
  - replaced with a `SemaphoreSlim` + `AsyncLocal<bool>` gate that is
  genuinely async-safe and still reentrant within one logical call chain.
  Every call site across every package and test project was updated to
  `await` accordingly, including `Assert.Throws`/`Record.Exception` calls
  that wrapped a now-async operation (xUnit's own analyzer flags these -
  `Assert.ThrowsAsync`/`Record.ExceptionAsync` are required, not optional,
  once the wrapped call is `Task`-returning: an exception thrown inside an
  `async` method lands in the returned `Task`, never thrown synchronously to
  the caller, so the old form would silently stop verifying anything).
- **BREAKING: the root namespace is now `Net.NowhereAtAll`, not `Net.Nowhereatall`**
  (every package: `Net.NowhereAtAll.Xfty`, `Net.NowhereAtAll.Xfty.Bogus`,
  etc.). C# namespaces are case-sensitive, so this breaks every `using
  Net.Nowhereatall...` in a consumer's own code - there is no compatibility
  shim; update the casing on upgrade.

### Removed

- **`SimpleRecordProvider<TRecord>` is gone.** It was an invented base class
  with no analog in the Apex original - a Provider that is "nothing but a
  Master Template" now implements `IRecordProvider` directly, holding its
  template as a field and delegating `CreateBundle` to
  `RecordFactory.CreateBundle` (the same composition idiom the bundled
  `ContactDataProvider`/`AccountDataProvider` already used). Every Provider
  that extended it - across the test suites and the add-on package README
  examples - was converted to that form; there is no inheritance left in the
  Provider layer.

### Fixed

- **`SharedAncestor` could crash under real concurrent access** —
  `ByName`/`Disabled` were a plain `Dictionary`/`HashSet`, and
  `SharedAncestorResolver`'s own `_running`/`InProgress` fields were
  unsynchronized. This port's own suite never hit it only because it
  disables xUnit's *default* collection parallelism; building `Xfty.Xunit.Test`
  without that same opt-out reproduced the crash immediately
  (`InvalidOperationException` from `Dictionary`'s internal state).
  `ByName`/`Disabled` are now `ConcurrentDictionary`s, `_manualResolution`
  is `volatile`, and the actual resolve-and-mutate work is serialized
  through a lock in `SharedAncestorResolver` covering every path that can
  trigger resolution. `SharedAncestorConcurrencyTest` reproduces the
  original crash reliably against the pre-fix code (confirmed by reverting
  the fix and re-running it) and passes reliably - 200 concurrent
  attempts, repeated runs - against the fix.

### Changed

- **`InsertMode.RelatedOnly` is gone, replaced by `.ExcludePrimaryIds()`/`.IncludePrimaryIds()`
  - an orthogonal setting on `RecordProvider`, not a mode.** Requested
  directly, then corrected twice over: excluding a call's own primary from
  persistence while its ancestors are still persisted normally is a
  different concern from *how* they're persisted, and baking it into
  `InsertMode` made the two impossible to combine - `RelatedOnly` and
  `Deferred` could never both apply to the same call, exactly the
  combination needed to build a primary with a deep (or multi-Provider)
  ancestor tree efficiently while leaving the primary itself un-Id'd.
  `Now` + `.ExcludePrimaryIds()` reproduces `RelatedOnly`'s exact original
  behavior (one-at-a-time ancestor inserts, so real trigger order is
  preserved - deliberately not batched, even under `.DepthBatched()`, on
  request); `Mock` + `.ExcludePrimaryIds()` reproduces the equally-short-lived
  `InsertMode.MockRelatedOnly` (added and removed the same day, never
  published); `Deferred` + `.ExcludePrimaryIds()` is the new capability
  neither `InsertMode` value could ever express. Mechanically, this
  *removed* special-casing rather than added it: `GenerationContext.ForRelated()`
  no longer transforms `InsertMode` at all (an ancestor simply inherits it
  unchanged, like every other mode always did), and `ExcludePrimaryIds`
  itself resets to `false` for every ancestor context, so it can never leak
  past the one call that set it. `RecordFactory.Persist` gained one guard
  clause; `DeferredInsertBuffer`/`DepthBatchedInserter` gained an excluded-index
  set threaded through the existing depth-batched machinery, unchanged
  otherwise. Proven end-to-end - the `Now`/`Mock` cases, the new `Deferred`
  case (an ancestor really inserted while the primary stays un-Id'd even
  after `Flush()`), a shared ancestor referenced under exclusion, and the
  explicit `.IncludePrimaryIds()` toggle - in `PersistenceGatewayTest`/
  `SharedAncestorIntegrationTest`. Found and fixed along the way: three
  existing tests that deliberately trigger `Flush()`'s "no gateway
  configured" throw left the shared static `DeferredInserter` registry
  permanently non-empty afterward (by design - a failed flush must never
  silently lose what was registered) with no way to clean up; added
  `DeferredInserter.ResetForTesting()` and wired it into all three. See
  [use/insert-modes.md](docs/use/insert-modes.md#excluding-the-primary---excludeprimaryids).
- **`Xfty.AutoBogus`** — the same pairing as `Xfty.AutoFixture`, for
  AutoBogus instead: `XftyAutoBogus.CreateFaker(lookup)`/
  `XftyAutoBogusOverride` points `faker.Generate<T>()` at a registered
  `RecordProvider` (an `AutoGeneratorOverride` with `Preinitialize =>
  false`, confirmed empirically to mean AutoBogus never constructs or
  populates its own instance before the override runs - nothing it
  generates is thrown away or overwritten by XFTY's result);
  `AutoBogusUnsetFieldFiller` reuses the same core `IUnsetFieldFiller`
  extension point `Xfty.AutoFixture` introduced, backed by an `IAutoFaker`
  instead of an `IFixture` - no further core change needed. One real
  behavioral difference from the AutoFixture filler, documented rather than
  papered over: AutoBogus never throws for a field that circles back on its
  own type (it self-limits recursion depth instead of AutoFixture's default
  `ThrowingRecursionBehavior`), so `AutoBogusUnsetFieldFiller` has no
  recursion-exception handling at all. `IAutoFaker`'s public surface is
  generic-only, so resolving a field's runtime `Type` needs one
  `MakeGenericMethod` call per distinct field type, cached after first use.
  Proven in `Xfty.AutoBogus.Test` (both directions, combined, exclusion,
  and the self-referencing-field case) reusing the same core
  `UnsetFieldFillerTest`. XFTY now pairs with all three of AutoFixture,
  Bogus, and AutoBogus - see
  [reference/comparison.md](docs/reference/comparison.md#could-xfty-pair-with-one-of-these-to-close-a-gap)
  and [use/autobogus.md](docs/use/autobogus.md).
- **`Xfty.AutoFixture`** — pairs XFTY with AutoFixture, two independent,
  non-mutually-exclusive ways: `XftyCustomization`/`XftySpecimenBuilder`
  points `fixture.Create<T>()` at a registered `RecordProvider` instead of
  AutoFixture's own generation (falling through to AutoFixture's default
  for anything unregistered); `IUnsetFieldFiller`/
  `AutoFixtureUnsetFieldFiller` lets AutoFixture fill in whatever fields a
  Provider's Master Template never configured at all (not a field XFTY
  resolved *to* null on purpose), via a new
  `MasterTemplate.IsConfigured(PropertyInfo)` and a new
  `RecordProvider.SetUnsetFieldFiller(...)` threaded through
  `GenerationContext` so it reaches generated ancestors too, each against
  its own unset fields. `IUnsetFieldFiller` itself lives in core `Xfty`
  with no dependency on AutoFixture (or anything else); only the adapter
  package does. Closes the "a real gap, a real design" auto-population
  pairing noted in
  [reference/comparison.md](docs/reference/comparison.md) - proven in
  `Xfty.AutoFixture.Test` (both directions, combined, exclusion, and a
  self-referencing field that never lets AutoFixture's recursion guard
  escape as an exception) and `UnsetFieldFillerTest` (the core contract,
  independent of any one filler implementation). See
  [use/autofixture.md](docs/use/autofixture.md).
- **Typed `RecordProvider<TRecord>`/`ChildProvider<TChild>`** — composed
  generic wrappers (both underlying classes are `sealed`) mirroring
  `MasterTemplate<TRecord>`'s own pattern: typed `Supply()`/`SupplyList()`
  with no cast at the call site, a `MasterTemplate<TRecord>`-style
  object-initializer indexer routed by the value's runtime type, and full
  fluent forwarding. Surfaced (and fixed) a latent gap in
  `ChildProvider.Put(PropertyInfo, object?)`, which always treated its
  value as a literal unlike the equivalent `MasterTemplate`/`RecordProvider`
  overloads - rewritten to dispatch on the value's runtime type the same
  way. Also added `LookupKey.Get<TRecord>()`,
  `FlavouredLookupKey.Get<TRecord>(flavour)`, and
  `IProviderLookup.Get<TRecord>()` (an extension method), eliminating
  `typeof(...)` at the two other largest clusters of that pattern in the
  codebase.
- **`Xfty.Xunit`** — `[IsolatesSharedAncestor]`, an attribute for a test
  class or method that resets `SharedAncestor`'s registry before and after,
  via xUnit's own `BeforeAfterTestAttribute` hook - the same effect as
  `SharedAncestor.ResetAllForTesting()`, wired up automatically instead of
  by hand. `IsolatesSharedAncestorAttributeTest` proves it prevents real
  leakage between two test methods that deliberately reuse the same name;
  the companion `SharedAncestorLeaksWithoutIsolationTest` proves the leak
  is genuinely real without it. Depends on `xunit.v3.extensibility.core`
  (not the full `xunit.v3` runner package - the correct, lighter dependency
  for a library shipping an xUnit extension rather than a test project
  itself).
- **`SharedAncestor.ResetAllForTesting()`** — clears the registry, every
  `Disable`d name, and the `ManualResolutionOnly()` flag in one call. Not
  automatic (nothing in .NET gives XFTY a per-test-method hook the way
  Apex's platform does), but a real, verified reset when called from a
  consuming project's own base test class or fixture. Also the fix for a
  worse, related problem: `ManualResolutionOnly()` had no unsetter of its
  own, and was previously untestable in this port's own suite for exactly
  that reason - `SharedAncestorResetTest` now exercises it end to end.
- **Multi-hop descendant reads** — `CopyFromDescendantExpression` gains a
  path-list constructor (`new CopyFromDescendantExpression([field1, field2,
  ..., sourceField])`), mirroring `CopyFromAncestorExpression`'s own
  multi-hop form. First matching child at every hop, `null` if any hop has
  no match. Needed `DeferredGraph` to expose a child's own flat index
  (`ChildIndicesOf`, `RecordAt`), not just the child record itself
  (`ChildrenOf`) - without that, not even a custom `IDeferredExpression`
  could walk a second hop. Closes half of the "First matching child, single
  hop only" limitation on the roadmap; reading an aggregate across children
  at one hop remains unbuilt as a bundled expression (already possible in a
  custom one, since `ChildIndicesOf`/`ChildrenOf` return every match).
- **`Xfty.Bogus`** — bundled `IValueExpression`s (`FakeFullNameExpression`,
  `FakeEmailAddressExpression`, `FakeStreetAddressExpression`,
  `FakeParagraphExpression`) producing realistic-looking values by wrapping
  Bogus, closing the "no realistic fake data" gap noted in
  [reference/comparison.md](docs/reference/comparison.md) - as a separate,
  opt-in package, so core `Xfty` gains no new dependency.
- **`Xfty.VectorDatabases`** — `RandomVectorExpression(int dimensions, float
  min, float max, bool normalize)`, filling a vector-database record's
  embedding field with a fixed-length array of random floats, optionally
  unit-length for cosine-similarity schemas. `KnownEmbeddingDimensions`
  bundles named dimension constants for popular embedding models
  (`OpenAiTextEmbedding3Small`, `CohereEmbedV3`, …). Structurally a vector,
  not a semantically meaningful embedding, and calling a real embedding API
  is a deliberate non-goal, not a gap - see
  [roadmap/vector-databases.md](docs/roadmap/vector-databases.md).
- NuGet packaging metadata (`PackageId`, `Version`, `Authors`,
  `PackageLicenseExpression`, embedded `README.md`, symbol packages) for
  every package in the solution; `dotnet pack` verified to produce a valid
  `.nupkg`/`.snupkg` pair for each. Publishing to nuget.org itself remains
  the maintainer's own manual step - see
  [contribute/packaging.md](docs/contribute/packaging.md).
- **pgvector proof** — `PgVectorPersistenceTest` (`Xfty.EntityFrameworkCore.Test`)
  proves a `Pgvector.Vector` column persists through the *existing,
  unmodified* `EfPersistenceGateway` - no new gateway code, just a package
  reference, a demo entity, and the `pgvector/pgvector:pg16` container image.
- **`Xfty.VectorDatabases.Qdrant`** (`0.1.0-preview.1`, not `1.0.0-beta.1` -
  see its own README) — `QdrantPersistenceGateway`, a real, working
  `IPersistenceGateway` through Qdrant's own client (`Qdrant.Client`,
  1.19.0) directly - no Microsoft.Extensions.VectorData, no Semantic
  Kernel connector.
- **`Xfty.VectorDatabases.MicrosoftExtensionsVectorData`** (`0.1.0-preview.1`)
  — `MevdPersistenceGateway`, a real, working `IPersistenceGateway` through
  Microsoft.Extensions.VectorData's abstract `VectorStore` - genuinely
  provider-agnostic (`GetDynamicCollection` etc. are declared on the base
  class itself, not any specific connector), so this package has zero
  Qdrant/Semantic-Kernel dependency even though its own test proves it
  against a real Qdrant container. Kept in a **separate** package from
  `Xfty.VectorDatabases.Qdrant` from the start, not combined even during
  this comparison - a consumer of the direct-client gateway shouldn't be
  forced to take a transitive dependency on MEVD and a still-preview
  connector it never uses. Answers "is a dedicated vector-DB gateway worth
  the extra abstraction, or is the vendor's own client just as easy":
  going through MEVD needed one real correction undocumented anywhere
  findable (a vector property's declared schema type must be the container
  type, `float[]`, not the element type, `float`); the direct gateway
  compiled and passed on the first attempt. One shared finding, checked on
  both paths rather than assumed from one: Qdrant's client requires `Guid`
  point ids, not `string` - a compile-time error on the direct path, a
  runtime one through MEVD, same real constraint either way - see
  [roadmap/vector-databases.md](docs/roadmap/vector-databases.md).

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
- **`MasterTemplate<TRecord>`** - an ergonomic, strongly-typed wrapper
  (collection-initializer syntax for a template) over the untyped engine
  underneath. (An earlier `SimpleRecordProvider<TRecord>` base class was
  removed before release - see [Unreleased] - Removed.)
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
