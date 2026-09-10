# Development History

XFTY **began** as a port of the Apex
[ExtremeApexTestDataFactory](https://github.com/nilvon9wo/ExtremeApexTestDataFactory).
That is why the early entries below read as porting decisions and why "Apex
did X" is a recurring reference point in them.

**It is no longer a port.** Once the translation to C# was finished, XFTY
became its own project. Later decisions are made for .NET consumers on their
own merits — a constraint that only ever existed because Salesforce imposed
it (string-only Ids, always-mutable reference types, schema-describe
validation) is not something a C# consumer inherits, and new abstractions
with no Apex counterpart are fair game when they serve .NET usage.

A running log of the decisions behind the code — design calls, corrections,
and things that turned out differently than planned once real test runs were
in front of them. History, not current-state documentation — see
[architecture](architecture.md) for what the engine looks like today. Several
entries describe an earlier design later reworked or reversed; read a topic
to its end before treating an early entry in it as still true.

Referenced directly from a few places in the code — `BundleEnricher`,
`PerformanceTest`, `DepthBatchedInserterTest` — where the rationale recorded
here is the actual reason behind a specific implementation choice, not
background color.

---

## Why port XFTY to C#

Brian raised (post `b24f389`, conversation lost to a closed window, recounted
2026-09-04): whether it's viable and worth the effort to port XFTY to C#.
Motivation is dual — showcase his C# ability, and XFTY has real features
AutoFixture and similar .NET test-data libraries don't offer, notably
**context-aware values** (see the org-seeding branch / beta's
`GenerationContext` work).

Points agreed so far (no build started, this is still at the "should we"
stage):
- **Drop the serialize/deserialize trick.** That's an Apex workaround (see
  the serialization-enrichment work) — C# has real reflection, so a port
  would build/inspect object graphs directly instead.
- **Deferred/depth-batched saving is optional in C#, not core.** Apex needs it
  because there's no ORM doing dependency-ordered inserts. A C# project on EF
  already gets that from EF's change tracker/SaveChanges — the deferred-save
  idea only pulls its weight for C# projects *without* EF.
- **No adapter/proxy layer needed.** Apex's `XFTY_DummySObject` wrapper exists
  because Apex has no dynamic POCOs the way C# does; in C# the library can
  just build and return plain POCOs directly.

**2026-09-04: decision made to proceed.** New repo location:
**`E:\projects\CSharp\XFTY`** (Brian is copying this whole Apex project there
as a starting scaffold/reference, then restarting Claude Code in that folder —
this log was the handoff since neither conversation state nor project-scoped
memory carries over automatically to a new working directory).

**Approach agreed (hybrid, not pure mechanical port, not pure from-scratch):**
Use the Apex repo as a **read-only reference/scaffold**, not a base to build
directly on top of.
- `values/`, `predicates/` (~35 classes) — pure logic, genuinely portable.
  Dumb search-replace + an intelligent fix pass is the right call here.
- `core/`, `engine/`, `relationships/` (partial) — concept ports. The
  Apex-side representation (`XFTY_DummySObjectBundle`/`DummySObjectMasterTemplate`,
  string-keyed dynamic SObject access) is a hand-rolled stand-in for what C#
  generics + real reflection give natively — rebuild the bundle/context
  representation on real generics/POCOs, keep the algorithms (context-aware
  value resolution, shared-ancestor wiring) conceptually intact. Use Apex as
  spec, not source.
- `enrichment/` (12 classes) — **the feature survives, the mechanism doesn't.**
  The Apex implementation (JSON serialize/inject) is the dead end, a workaround
  for Apex having no real reflection over dynamic SObject shape. The C# port
  keeps enrichment as a capability but implements it directly via reflection
  instead of serialize/deserialize.
- `seeding/` (the `@IntegrationTest` DML-without-rollback hack) — **true dead
  end, zero C# analog.** C# unit tests just call `SaveChanges`/persist
  normally, no special hack needed.
- `persistence/` (`IdMocker`, `DeferredInsertBuffer`/`DepthBatchedInserter`) —
  **corrected from an earlier draft of this plan that wrongly called these
  Salesforce-only.** `IdMocker`'s purpose (placeholder FK values before/without
  a real insert) is real in C# too — pure in-memory unit tests never get a
  real identity-column round-trip. The depth/topological-layering insert
  algorithm is a real capability for non-EF paths AND for EF-with-triggers-or-
  stored-procs (EF doesn't remove trigger-order concerns). Only
  `GovernorBudget`'s specific Salesforce limit numbers are dead weight — the
  general "cap batch size" concept even has a real .NET target (SQL Server's
  ~2100-parameter-per-query ceiling).
- `lookup/`'s RecordType-*specific* matching logic doesn't port, but the
  general flavoured/keyed-lookup mechanism (`LookupKey`/`FlavouredLookupKey`/
  `ProviderLookups` — "pick a named variant of a provider") does, and EF's TPH
  discriminator pattern (or a plain discriminator column on a non-EF schema)
  gives it a real target to drive off of.
- `providers/` (`DefaultAccountDataProvider` etc.) — Salesforce-standard-object
  specific, no direct analog; becomes new demo-domain code either way (a
  Contact/Account-shaped demo domain is a plausible deliberate choice for
  clean 1:1 comparison against AutoFixture examples).
- Tests: carry over the *scenarios* (what's verified) as the spec for new
  xUnit tests, not the Apex `Assert`/AAA syntax verbatim.

**Conventions to match** (found by inspecting a prior C# project as a
template — not confirmed to be Brian's most recent/best-configured C#
project, treated as one reasonable precedent, not gospel):
- `net10.0`, `ImplicitUsings`/`Nullable` enabled.
- Test stack: `xunit` + `xunit.runner.visualstudio` + `NSubstitute` +
  `coverlet.collector` + `Microsoft.NET.Test.Sdk`. **Not** FluentAssertions,
  despite matching the template - Brian caught (2026-09-04) that FluentAssertions
  8.x is Xceed-licensed, free for non-commercial use only, and didn't want a
  license-encumbered dependency for something as trivial as an assertion,
  especially one potential users of a portfolio library would shy away from.
  Plain xUnit `Assert.*` instead - no extra dependency, and it happens to
  read closer to Apex's own `Assert.*` calls than a fluent chain would.
- `LanguageExt.Core` in the main project — matches this project's own style
  rule ("never use for/while, prefer functional") which plain C# can't really
  honor for stateful code without a library like this (`Option`/`Either` etc.).
- Project split: `<Name>.Core` + `<Name>.Core.Test`, `RootNamespace`/
  `AssemblyName` both set explicitly, `ProjectReference` wiring, `<Using
  Include="Xunit" />` global usings block.
- Strict `.editorconfig` at repo root encoding the style rules as analyzer
  errors (`dotnet_style_qualification_for_*=true:error`,
  `dotnet_style_parentheses_*:error`, etc.).
- Full style rules: this port's own house rules, folded into
  [coding-standards](coding-standards.md) (one-expr-per-line, verbs/nouns, no
  inner classes, ≤100-line classes, ≤10-line methods, never nest >2 deep).

**2026-09-04 (scaffolding session): decisions resolved.**
- Root namespace: **`Net.NowhereAtAll.Xfty`** (a placeholder-domain pattern,
  keeps the short "XFTY" branding rather than the long folder name).
  Solution/projects are `Xfty.slnx`, `Xfty.Core`, `Xfty.Core.Test`.
- Demo domain for `providers/`-equivalent showcase code: **Contact/Account**,
  mirroring the Apex standard objects 1:1 for clean before/after comparison.
- Repo strategy: **entirely new git repo, not a branch off the Apex history.**
  The Apex checkout's `.git` was deleted and reinitialized fresh in
  `E:\projects\CSharp\ExtremeCSharpTestDataFactory` — no shared history, no
  `origin` pointing at ExtremeApexTestDataFactory. The Apex source tree
  (`force-app/`, `docs/`, etc.) stayed in the working tree as the read-only
  reference/scaffold described above; it just no longer carries Apex commit
  history into the new repo.
- Scaffold built and verified this session: `Xfty.slnx` (slnx format, a
  `Solution Items` folder holding `.editorconfig`/style-rules/this log),
  `Xfty.Core` (net10.0, `LanguageExt.Core`), `Xfty.Core.Test` (xunit,
  FluentAssertions, NSubstitute, coverlet.collector, `Microsoft.NET.Test.Sdk`,
  project reference to `Xfty.Core`). `dotnet build`/`dotnet test` both clean
  under the strict `.editorconfig` analyzers.

**Correction to the `enrichment/` line above:** the *feature* survives the
port — only the Apex JSON-serialize/deserialize **mechanism** is the dead end
(a workaround for Apex lacking real reflection). The C# port implements
enrichment directly via reflection instead.

**Testing conventions carry over from Apex in spirit, not literally.** Brian
confirmed (2026-09-04) `docs/contribute/coding-standards.md` holds "just as
true for C#", then clarified same day: it's the *spirit* of the doc that
survives, not its Apex-syntax examples — idiomatic C# is preferred throughout,
and naming conventions are expected to be the biggest visible difference (e.g.
PascalCase test method names instead of the `test`-prefixed camelCase Apex
uses, `[Theory]`/`[InlineData]` instead of a runner called from thin `@IsTest`
data-row methods). (This entry originally said FluentAssertions `.Should()`
here - superseded 2026-09-05, see the FluentAssertions-removal entry below;
plain xUnit `Assert.*` turned out to read closer to Apex's own `Assert.*`
anyway.) The one piece kept **literal**, not just in spirit: the AAA
structural comments — `// Arrange`, `// Act`, `// Assert`, and
`// Sanity Check` (a pre-Act assertion that arranged state matches what the
test assumes) — stay verbatim in every xUnit test. Everything else in the
"Testing and coverage" section (one test class per unit, one behaviour per
test method, the Act is exactly one statement, parameterised tests as a thin
data-row layer over a shared AAA-commented runner, catch the specific
exception type) carries over as *intent*, expressed idiomatically in C#.

## 2026-09-04: `predicates/` ported (first completed module, unattended)

Brian stepped away and asked me to get as much done autonomously as possible,
after confirming there were no more real open decisions on the overall
approach. `predicates/` is done: all 11 source classes + their test classes,
`dotnet build`/`dotnet test` clean, 43/43 passing. `values/` is next but not
started - see "Still open" at the end of this file.

**Design decisions made along the way (nothing here was previously agreed;
flagging for review, not asking permission mid-flight since nobody was
around):**

- **`SObjectField` → a real typed field accessor, not a string/reflection
  stand-in.** Apex's predicates take a `Schema.SObjectField` token and read it
  off an untyped `SObject` via `record.get(field)`. C# has no such dynamic
  record base type, so `IRecordPredicate<TRecord>` is generic per record type,
  and the field predicates (`FieldEqualToPredicate<TRecord,TValue>` etc.) take
  a real `Func<TRecord,TValue>` accessor - e.g. `a => a.Industry` - not a
  string or a bare `PropertyInfo`. This is strictly more type-safe than the
  Apex original, not a compromise. (Considered `Expression<Func<TRecord,
  TValue>>` instead, for future EF/LINQ-provider pushdown - deferred: nothing
  today would consume the expression tree, only the compiled delegate. Easy
  to widen later if `lookup/`'s EF integration wants it; flagging here so it
  isn't forgotten.)
- **`XFTY_ValueComparison` doesn't exist in C# - `Comparer<TValue>.Default`
  replaces its whole runtime type-sniffing switch** (`instanceof Decimal`,
  `instanceof Date` before `instanceof Datetime` because Apex `Date` is also
  `instanceof Datetime`, etc.). Generics make the numeric/date/lexical
  dispatch unnecessary - `TValue` is one concrete type per call site, so
  `Comparer<TValue>.Default.Compare(...)` is correct for numbers, `DateTime`,
  `string`, or anything else `IComparable` for free. Deliberately **not**
  constrained to `TValue : IComparable<TValue>`: a nullable value type like
  `int?` can never satisfy that constraint (`Nullable<T>` doesn't implement
  `IComparable<Nullable<T>>`), and the demo/record fields are routinely
  nullable, so the constraint is dropped and the null guard that already
  existed (matching Apex's "null is never greater/less") runs first.
  `XFTY_ValueComparisonTest`'s scenarios are still covered - just as inline
  cases inside `FieldGreaterThanPredicateTest`/`FieldLessThanPredicateTest`
  rather than a standalone class, since there's no longer a standalone class
  to test.
- **`XFTY_DummySObjectFtyProviderException` → `XftyConfigurationException`.**
  One shared, C#-idiomatic name (no `XFTY_` prefix - real namespaces make that
  Apex workaround unnecessary) for the "loud, named error"
  `coding-standards.md` asks for. Lives at `Net.NowhereAtAll.Xfty.Core` root
  since every future module needs it, not just `predicates/`.
- **Demo domain scaffolding started:** `Xfty.Core/Demo/Account.cs`, a minimal
  POCO (`Name`, `Industry`, `Type`, `NumberOfEmployees`, `AnnualRevenue`) with
  only the fields the ported predicate tests exercise. Will grow when
  `providers/`-equivalent work starts; not meant to be exhaustive yet, and a
  `Contact` counterpart doesn't exist yet either.
- **`XFTY_PredicatesTest`'s `XFTY_FlavouredLookupKey` scenarios were dropped**,
  not ported - they exercise `lookup/`, which isn't ported yet. Only the
  facade-delegation tests (`AllOf`/`AnyOf`/`Negate` wiring) came across in
  `PredicateFactoryTest`. Revisit once `lookup/` lands.
- **Folder/namespace shape:** `Xfty.Core/Predicates/` →
  `Net.NowhereAtAll.Xfty.Core.Predicates` (mirrors the Apex package folder,
  matches `dotnet_style_namespace_match_folder`). `XFTY_Predicates` (the AND/
  OR/NOT facade) became `PredicateFactory` and `XFTY_FieldPredicate` became
  `FieldPredicateFactory` rather than keeping the Apex names verbatim - a
  class named the same as its own containing namespace segment
  (`Predicates.Predicates`) was the alternative and read worse; `...Factory`
  also matches `coding-standards.md`'s own "name a doer class for what it
  produces" rule (its `XFTY_RecordCloneFactory` example).

## 2026-09-04: `values/` partially ported (plain expressions only) - then blocked on local test execution

**What's done:** `IValueExpression` (→ `XFTY_ValueExpressionIntf`) and its 7
self-contained implementations - `LiteralExpression`, `IncrementingDecimalExpression`,
`IncrementingStringExpression`, `UniqueStringExpression`, `UniqueEmailExpression`,
`UniqueStringOfLengthExpression`, `UniqueAcrossRunsExpression` - plus one test
class per implementation (splitting Apex's single grab-bag `XFTY_ValueExpressionTest`,
matching the one-test-class-per-unit shape already used for `predicates/`).
`dotnet build` is clean (0 warnings/errors, full analyzer set). **Not** ported:
`XFTY_ContextAwareExpressionIntf` and `XFTY_DeferredExpressionIntf` (and their
implementations: `CopyFromAncestorExpression`, `CopyFromSiblingExpression`,
`CopyFromDescendantExpression`) - both depend on `XFTY_GenerationContext`/
`XFTY_DeferredGraph`, which are `core/`-level types that don't exist in C# yet.
Porting those now would mean designing the bundle/context representation
unattended and blind (no test execution to check it against, see below) -
too big a call to make solo mid-flight; natural next unit of work once `core/`
is scoped, not a blocker for anything else.

**Design decisions, same spirit as `predicates/`:**
- `IncrementingStringExpression`'s two Apex constructors (Apex has no default
  parameter values) collapsed into one C# constructor with a default parameter
  (`separatePrefix = SeparatePrefix`) - the `SeparatePrefix`/`DontSeparatePrefix`
  named-constant pair stays, per `coding-standards.md`'s "magic booleans get a
  name" rule.
- `UniqueStringOfLengthExpression`'s base-26 `for` loop became a recursive
  `GenerateNextString` - no `for`/`while` anywhere, per the style rules.
- **A real Apex/C# static-lifetime difference, not just a style choice:** Apex
  resets `static` fields before every `@IsTest` method; a C# `private static`
  field on `UniqueStringExpression`/`UniqueEmailExpression`/`UniqueStringOfLengthExpression`
  persists for the whole test-process run, across every test in the assembly.
  None of the ported tests depend on an *exact* counter value except the Apex
  original's `countsSeparatelyPerLength` test (which asserted literal `'AAA'`/
  `'AAAA'` starting values) - its C# equivalent uses lengths no other test in
  the class touches and asserts *relative* behaviour (shared counter within a
  length, independent counters across lengths) instead of exact literals, so
  it isn't order-dependent across a whole `dotnet test` run. Flagging this
  since it's the kind of thing that should be loud, not silently patched over.

**Blocked on: local test execution, not on anything about the code.**
Partway through this module, `dotnet test` started failing with `An
Application Control policy has blocked this file` loading
`Xfty.Core.Test.dll` from `testhost.exe`. Confirmed via
`Microsoft-Windows-CodeIntegrity/Operational` (events 3077/3033/3118): this is
**Windows Smart App Control** (`VerifiedAndReputablePolicyState = 1`,
i.e. Evaluation mode) blocking an unsigned, freshly-compiled local dev DLL
from being reflection-loaded (Policy ID `{0283ac0f-fff1-49ae-ada1-8a933130cad6}`).
It is **not deterministic** - `predicates/` ran clean at 43/43 (and the
original scaffold at 45/45) earlier in this same session on the same kind of
unsigned local DLL, then this same command started being blocked with no code
change that plausibly explains it. Retried ~5 times (including a full
`bin`/`obj` clean rebuild) without success.

I did not attempt to change this myself - Microsoft's own guidance is that
Smart App Control, once turned off, **cannot be turned back on without
reinstalling Windows**, so that's squarely Brian's call, not mine to make
unattended. `values/`'s new tests are therefore **build-verified (0
warnings/errors under the full analyzer set) and manually traced through by
hand against the Apex originals' expected behaviour, but not
execution-confirmed** - unlike `predicates/`, which has an actual 43/43 green
run behind it. Worth running `dotnet test` yourself first thing to confirm,
once Smart App Control is sorted out (Windows Security → App & browser
control → Smart App Control, or check whether this machine has an
organization-managed code integrity policy layered on top - the "Enterprise
signing level requirements" wording in the event log is oddly enterprise-y
for what looks like a personal dev box).

**Still open when picked back up:** `core/`'s `GenerationContext`/bundle
representation (needed to unblock `XFTY_ContextAwareExpressionIntf`, and
therefore `CopyFromAncestor/Sibling/DescendantExpression`) - the next real
design decision, deliberately not attempted unattended. Confirm `values/`'s
tests actually pass once `dotnet test` works again locally.

## 2026-09-05: Smart App Control block cleared; FluentAssertions dropped

Brian ran `dotnet test` himself: **59/59 passing** - the Smart App Control
block from the previous session is gone (whether it cleared on its own or was
genuinely non-deterministic, as suspected, is unclear either way; not worth
chasing further now that it's not blocking).

He also caught something the test run surfaced: FluentAssertions 8.8.0 prints
an Xceed commercial-license notice at test-run start. FluentAssertions
versions 8+ are free for non-commercial use only. He didn't want a
license-encumbered dependency for something this trivial, especially one
potential users of a portfolio/showcase library would see and be put off by.
**Removed entirely** - not swapped for another fluent library (e.g.
AwesomeAssertions, the MIT-licensed post-license-change fork) - plain xUnit
`Assert.*` covers everything needed and reads closer to Apex's own `Assert.*`
calls than a fluent chain did anyway. All 16 existing test files (9
`predicates/`, 7 `values/`) rewritten; `Assert.Throws<T>(...)` capturing the
exception is a tighter match for coding-standards.md's "capture the throw in
Act, assert on it in Assert" than `Action act = ...; act.Should().Throw<T>()`
was. 59/59 still green after the rewrite.

## 2026-09-05: `predicates/` reworked - reflection-based, non-generic, matches Apex 1:1

Brian pushed back hard on the previous session's `predicates/` design:
*"I'm not clear why you feel the need to redesign it... C# can do everything
Apex can do, so the original design should have been fine... I fear you are
trying to be too creative rather than just faithfully porting XFTY to C#."*
Fair - and specifically correct about the mechanism. `SObject`/`SObjectField`
is a *single* dynamic-record type usable across every Salesforce object, so
`XFTY_SObjectPredicateIntf` isn't generic - it doesn't need to be. The
faithful C# equivalent of "dynamically read a field off any record type" is
**reflection**, not generics: `PropertyInfo` standing in for `SObjectField`,
`object` standing in for `SObject`. That's literally what the very first
version of this plan said ("C# has real reflection... build directly instead")
- the earlier session just didn't apply it here, and instead invented
`IRecordPredicate<TRecord>` + `Func<TRecord,TValue>`, which is structure Apex
never had.

**Confirmed with Brian before reworking** (already-shipped, tested, pushed
code - didn't want to guess wrong a second time): reflection-based,
non-generic, and rework `predicates/` now rather than only going forward.

**What changed:**
- `IRecordPredicate<TRecord>` → `IRecordPredicate` (non-generic),
  `bool IsSatisfiedBy(object? record)`.
- `Func<TRecord,TValue>` field accessors → `PropertyInfo`, obtained via a new
  `Field.Of<TRecord>(string propertyName)` helper (the direct equivalent of
  how `Account.Industry` resolves to an `SObjectField` token in Apex -
  reflection instead of a compiler-built-in, since C# has no field-token
  literal - but callers still write `Field.Of<Account>(nameof(Account.Industry))`,
  not a raw string, so a typo is a compile error via `nameof`, not a runtime
  surprise).
- `XFTY_ValueComparison` **restored** as `ValueComparison` (numeric/DateTime/
  lexical dispatch, `Math.Sign(...)`-normalized to -1/0/1) - deleting it only
  made sense under the generic design, where `TValue` was one concrete type
  per call site; with `object`-typed fields again, the dynamic dispatch is
  necessary again, exactly like Apex. `ValueComparisonTest` restored too.
- The invented `FieldPredicateBase` abstract class is **gone** - Apex has no
  such base (each predicate class independently holds its own field +
  comparison value), and duplicating one ternary line across 4 small classes
  is cheaper than inventing structure Apex doesn't have.
- `AllOfPredicate`/`AnyOfPredicate`/`NegationPredicate` lost their `<TRecord>`
  parameter along with the interface - otherwise unchanged.
- Kept as-is (not walked back): `PredicateFactory`/`FieldPredicateFactory`
  names (vs. `XFTY_Predicates`/`XFTY_FieldPredicate`) - that was a naming
  choice avoiding a class sharing its own namespace segment's name, not a
  structural invention, and naming was always expected to be "the biggest
  visible difference" per the testing-conventions discussion.

All 9 `predicates/` test files rewritten to match (`Field.Of<Account>(...)`
instead of a lambda selector), plus the new `ValueComparisonTest`. `dotnet
build`: 0 warnings/errors. `dotnet test` was blocked again by the same
intermittent Smart App Control issue, right up through the commit - tried
Bash, PowerShell, and a clean `bin`/`obj` rebuild with no luck. Every line was
hand-traced against the Apex originals' expected behaviour before committing;
a retry shortly after landed a clean **70/70**, confirming the trace.

**Going forward into `core/`:** same philosophy - class-for-class, method-
for-method, `PropertyInfo`/`object` wherever Apex used `SObjectField`/
`SObject`, no new abstractions Apex doesn't have. Do not generalize, do not
delete Apex classes in favor of "simpler" C# equivalents, unless explicitly
agreed first.

## 2026-09-05: enforce code style in build; demo domain grows; `core/` begins

Brian flagged three more things, then went to bed asking for as much progress
as possible unattended:

1. Analyzer errors he was seeing (CA1859, IDE0021, IDE0028, IDE0046, IDE0306,
   IDE2003, IDE2006) that `dotnet build` never caught. Root cause: without
   `EnforceCodeStyleInBuild`, the strict IDE0xxx/error-severity rules in
   `.editorconfig` were only ever checked by an IDE's live analysis or
   `dotnet format` - never `dotnet build` or CI. Added a `Directory.Build.props`
   (`EnforceCodeStyleInBuild` + `AnalysisLevel=latest`) so every project gets
   this from now on, and fixed everything that surfaced: expression-bodied
   constructors, guard-if-then-throw collapsed to throw-ternaries,
   `ValueComparison.Compare` rewritten as a switch expression (also clears
   the blank-line/IDE2003 violations the if-chain caused). CA1859/IDE0028/
   IDE0306/IDE2006 specifically didn't reproduce even with this on - possibly
   IDE-only inspections beyond what these analyzers catch, or he saw them
   before the predicates rework; worth a fresh look in-IDE.
2. Demo domain: `Account`'s properties are now `init`-only (proves
   reflection-based field access doesn't care about setter accessibility),
   and it gained `Site`/`Description` (needed by the `GenerationContext`
   work below). Added `Contact` as a positional `record class` - the other
   half of the Contact/Account pair, and a second common C# property-
   declaration shape (compiler-generated `init`-only properties, value
   equality). `RecordShapeFieldAccessTest` proves predicates read both
   shapes identically.
3. *"It is hard to give you feedback when the most important part is still
   not even started."* Fair - `core/`'s `GenerationContext` is what makes
   context-aware values (this port's actual differentiating feature vs.
   AutoFixture) work, and nothing there existed yet.

**`core/`'s real scope, once actually read class-by-class:** `GenerationContext`
alone touches `XFTY_DummySObjectBundle`, `XFTY_PathValue`/`XFTY_PathTargetValue`,
`XFTY_DummySObjectMasterTemplate`, the `relationships/` interfaces
(`XFTY_DummyDefaultRelationshipIntf`, `XFTY_SharedRelationshipIntf`), and
`lookup/` (`XFTY_LookupKeyIntf`, `XFTY_ProviderLookups`) - which themselves
pull in more. Fully porting the engine that walks a Master Template and
actually builds a record graph (`XFTY_DummySObjectProvider`,
`XFTY_SObjectChildProvider`, `XFTY_AncestorPathWalker`, `XFTY_BundleMerger`,
`XFTY_DeferredValueQueue`) is genuinely a multi-session undertaking, not
something to rush unattended just to say `core/` was "started."

**Decision: scope tonight to one complete, real, working vertical slice of
context-aware values instead of a pile of half-wired plumbing.** Landed:
`GenerationContext` (deliberately partial - only `RecordBeingBuilt` and
`ValueFieldPass` exist so far, everything else the Apex original carries
(Provider Lookup, insert mode/inclusivity, `bundleSoFar`, forced-relationship
paths, path-value overrides, the cycle guard, the batched-insert flag) is
added once the types it depends on are ported - see the class's own doc
comment), `ValueFieldPass`, `IContextAwareExpression`, and
`CopyFromSiblingExpression` fully working end-to-end with `SiblingValue`'s
loud-throw-on-still-pending behavior intact. This is a genuine, if narrow,
slice of the actual feature working - not a stub.

Tests ported from `XFTY_ContextAwareExpressionTest`/`XFTY_GenerationContextTest`:
only the ones that don't need the full provider engine (`SiblingValue`'s two
behaviours, `CopyFromSiblingExpression`'s constructor guard, its
outside-the-value-pass throw, its two-interfaces-not-one type check, and the
plain/generated-null-sibling cases) - built directly against a
`GenerationContext` rather than through `provider.Put(...).Supply()`, which
doesn't exist yet. The Apex tests that drive a Provider (`sees an earlier
context-aware sibling`, `does not override a value the override template
supplied`, both "throws" tests reached through `.supply()`, everything under
`XFTY_CopyFromAncestorExpression`, the custom-expression examples) are **not
ported yet** - they need the engine. 80/80 passing (build+test both clean).

**Captured but not built - a design input for the eventual Provider/Master
Template public API, from Brian directly:** C# gives custom types real
collection-initializer and indexer ergonomics (`{ }`/`[ ]`), which Apex has
no equivalent for. He floated something like initializing a Provider/template
with `{ x => x.Foo = new IncrementingStringExpression("hello"), ... }` and
looking up a field's configured value with an indexer (`account[x => x.Bar]`).
This is real and worth pursuing once `DummySObjectMasterTemplate` is
ported - **but it's new structure Apex doesn't have, so it needs an explicit
"yes, build this" the same way the reflection-vs-generics question did,** not
a silent addition. Flagging here so it survives to that point.

## 2026-09-05: the whole generation engine, mechanically ported in one sitting

After the fidelity correction above, Brian pushed back once more, much more
forcefully - the short version: stop treating every remaining class as a
fresh design question, just convert Apex to C# class-for-class and fix what
breaks, the way the very first message of this session actually asked for.
Fair, and overdue. Everything below was built the same session as a single
continuous mechanical pass: read a batch of Apex source, translate it
directly (`SObjectField`→`PropertyInfo`, `SObject`→`object`, `Map`→
`Dictionary`, `Set`→`HashSet`, Apex inner classes and nested enums extracted
to top-level types - the C# style rules don't allow either), build, fix
whatever the analyzers or compiler flagged, move to the next batch. No
pauses to ask permission between classes.

**Landed, in dependency order:**
- `core/`: `InsertMode`/`InsertInclusivity` enums, `AncestorCycleGuard`,
  `InverseAlignment`, `PathTargetValue`/`PathValue` (`Kind` → top-level
  `PathTargetValueKind`), `GenerationContext` (completed - every field and
  derivation method now, not just the partial `RecordBeingBuilt`/
  `ValueFieldPass` slice from before), `Bundle` (`ChildEntry` → top-level
  `BundleChildEntry`), `AncestorPathWalker`, `BundleMerger`,
  `DeferredValueQueue` (`Entry` → `BundleDeferredEntry`), `DeferredGraph`
  (`ParentLink` → `DeferredGraphParentLink`), `RecordCloneFactory` (Apex's
  `SObject.clone(...)` has no C# equivalent - copies every property via
  reflection instead, same guarantee), `RecordFactory` (from
  `XFTY_DummySObjectFactory`), `RecordProvider` (from
  `XFTY_DummySObjectProvider` - the main public entry point), `IRecordProvider`.
- `lookup/`: `ILookupKey`, `LookupKey`, `IProviderLookup`, `ProviderLookups`
  (`MapBackedLookup`/`LookupException` → top-level), `ISharedAncestorDefaults`,
  `FlavouredLookupKey`. **Dropped, not faked:** `RecordTypeLookupKey`/`Intf`,
  `RecordTypeMatching`, `RecordTypeDataProvider` - genuinely Salesforce
  schema metadata (record types) with no C# analog; this was already the
  agreed carve-out from the very first session ("a real target would be EF's
  TPH discriminator, not attempted here").
- `relationships/`: `IDefaultRelationship`, `ISharedRelationship`,
  `DefaultRelationship`. **Not ported:** `SharedAncestor`/
  `SharedAncestorProvider` - both need `XFTY_SharedAncestorResolver`, which
  needs the depth-batched insert machinery already agreed optional/non-core
  for this port (`XFTY_DeferredInsertBuffer` et al).
- `engine/`: `PlainValueFiller`, `ContextAwareValuePass`, `RelationshipForcer`,
  `PathValueApplier`, `LookupWiring`, `SharedRelationshipWiring`,
  `AncestorGenerator`, `ChildProvider` (from `XFTY_SObjectChildProvider`;
  `PendingPut`'s kind → top-level `ChildProviderPendingPutKind`/
  `ChildProviderPendingPut`). **Not ported:** `DescendantValuePass`
  (needs `XFTY_DepthBatchedInserter.ParentLink`/`XFTY_PendingDeferredValue`,
  same DEFERRED machinery carve-out), `GovernorBudget` (Salesforce-limit
  numbers, already agreed dead weight from session 1).
- `persistence/`: `IdMocker` only (the DEFERRED/depth-batched buffer classes
  are the same carve-out again).
- `values/`: `CopyFromAncestorExpression`, `CopyFromDescendantExpression` -
  both were blocked at the start of this session on `Bundle`/`DeferredGraph`
  not existing; both do now.

**A genuine capability gap, not a design choice - noted where it bites:**
Apex's schema describe gives real metadata C# reflection cannot: `SObjectField
.getDescribe().getReferenceTo()` tells you what type a lookup field points
at; a plain C# `string AccountId` property has no such link to `Account`.
`ChildProvider`/`RecordProvider` dropped the `assertFieldPointsAt`-style
validation this enabled - a misconfigured field now surfaces as a wrong or
null value instead of failing fast at configuration time. This is different
in kind from the reflection-vs-generics question earlier: there is no
faithful C# equivalent available at all, not just a less-generic one.

**Proven working, not just compiling:** `RecordProviderIntegrationTest` and
the `CopyFromAncestor/DescendantExpressionTest`s exercise the real engine -
two demo `IRecordProvider`s (`AccountDataProvider`, `ContactDataProvider`)
wired through a real `IProviderLookup`, a required relationship generating a
parent, `InsertInclusivity.None` correctly skipping it, quantity producing
one distinct parent per child, an override template winning over the default
filler, and downward child-collection generation wiring every child to the
same generated parent - not unit tests of one class in isolation. 97/97
passing (build clean; execution blocked by the recurring Smart App Control
issue partway through, cleared on retry, same pattern as every previous
occurrence this project).

**What's left, for real this time:** the DEFERRED/depth-batched persistence
path (`DeferredInsertBuffer`, `DeferredInserter`, `DepthBatchedInserter`,
`PendingDeferredValue`, `IndexedRecord`, `GovernorBudget`, `DescendantValuePass`)
and everything that depends on it (`SharedAncestor`/`SharedAncestorProvider`,
`XFTY_RecordTypeDataProvider`'s SOQL-backed sibling has no target either) -
all optional/non-core per the session-1 plan, not attempted here. A real
`InsertMode.Now` needs an actual persistence layer (EF or otherwise) that
does not exist in this port yet - `RecordFactory.Persist` throws
`NotSupportedException` for it rather than silently doing nothing.
`enrichment/` (the reflection-based rebuild, a separate track per session 1)
and `providers/`-equivalent demo-domain breadth beyond the two Account/
Contact providers here are the natural next work.

**2026-09-05: the "what's left" list above got built.** Full mechanical port
of the DEFERRED/depth-batched persistence path (`IndexedRecord`,
`PendingDeferredValue`, `DepthBatchedInserterParentLink` - confirmed the same
type Apex's `XFTY_DeferredGraph` reuses, not a separate one,
`CyclicGraphException`, `DepthBatchedInserter`, `DeferredInsertBuffer`,
`DeferredInserter`, `DescendantValuePass`) and the `SharedAncestor` subsystem
(`SharedAncestor`, `SharedAncestorProvider`, `SharedAncestorFieldValue`,
`SharedAncestorResolver`) - a flyweight registry so every relationship
referencing the same shared-ancestor name resolves to one generated record.
`RecordProvider.SupplyBundle()` now resolves shared ancestors up front and
fully branches on `DepthBatched()`/`ForceStructuralChildGeneration()`/
insert mode the way Apex's `supplyBundle()` does. `GovernorBudget` remains
not ported - no C# analog for `Limits.getCpuTime()` etc. `seeding/` remains
out of scope per explicit instruction.

Found and fixed a real cross-test race, not a port bug: xUnit runs test
classes in parallel by default, but `SharedAncestor`'s static registry
(deliberately static - the closest equivalent to Apex's per-test-reset
statics, see its doc comment) was never built to be thread-safe, because
Apex test methods never run concurrently with each other either. One test's
in-flight resolution was tripping another, unrelated test's cycle detector.
Fixed by adding `[assembly: CollectionBehavior(DisableTestParallelization =
true)]` to `Xfty.Test` - serializing the run restores the single-threaded-
per-org semantics the design already assumes, rather than bolting locking
onto a registry Apex never needed to make thread-safe.

Renamed `Xfty.Core` -> `Xfty` and `Xfty.Core.Test` -> `Xfty.Test` (folders,
`.csproj` files, `AssemblyName`/`RootNamespace`, `Xfty.slnx` project paths).
The project wasn't big enough to justify per-feature assemblies, so
`Net.NowhereAtAll.Xfty.Core.Core` (the `Core/` subfolder namespace, doubled
by the project name) was pure stutter. `Net.NowhereAtAll.Xfty.Core` now
means only the `Core/` subfolder - the project root namespace is
`Net.NowhereAtAll.Xfty`. 102/102 tests passing, stable across repeated runs
with parallelization disabled.

**2026-09-05, later the same day: `enrichment/` ported, per explicit
instruction ("USE REFLECTION FOR INJECTION").** Full mechanical port of
`InjectConfig` (+ `AncestorValue`/`ChildValue`, extracted from Apex's nested
classes), `EnrichmentSelection`, `EnrichmentTarget`, `ForcedValues`,
`PathKey`, `QueryableShapeValidator`, `SObjectInjector`, `BundleEnricher` (+
`EnrichmentPosition`, extracted from its nested `Position`). `Bundle` gained
`Inject`/`InjectAll`/`InjectAllParents`/`InjectAllChildren`, matching Apex's
`XFTY_DummySObjectBundle`.

**The actual redesign, done deliberately per instruction, not asked about:**
Apex's `XFTY_SObjectInjector` writes a populated relationship / child
subquery / read-only field by round-tripping the whole record list through
`JSON.serialize`/`JSON.deserialize`, because `SObject.put(...)` rejects those
outright. The C# `SObjectInjector` instead clones each record (via the
existing `RecordCloneFactory`) and sets every grafted property directly with
`PropertyInfo.SetValue`, the same `init`-bypass `IdMocker` already relies on.
`XFTY_BlobCarrier` - which exists only to shepherd a `Blob` safely through
that JSON round-trip - has no reason to exist here: reflection sets any
.NET type uniformly, so it was dropped outright, not adapted.

**A second, smaller reflection substitution, also flagged rather than
silently designed around:** Apex's `XFTY_InjectionPathResolver` gets the
relationship name to graft under from the schema describe
(`field.getDescribe().getRelationshipName()`, `SObjectType.getChildRelationships()`)
- metadata a plain C# property does not carry. In its place: a lookup field
named `XId` grafts onto a sibling `X` property on the same type (`Contact.AccountId`
-> `Contact.Account`); a child collection grafts onto whichever property on
the parent type holds a `List<T>` of the child's own type (`Contact` ->
`Account.Contacts`), and it is an error if zero or more than one such
property exists. Exercised end-to-end this required adding `Contact.Account`
and `Account.Contacts` navigation properties to the demo domain - a plain
POCO needs somewhere to write injected data that an `SObject` gets for free.

**Also carried over, not revisited:** the `QueryableShapeValidator`'s
SOQL-hop-count guard rails (`parentDepth` <= 5, `childDepth` <= 1 unless
`AllowDeeperGraph()`) - the exact numeric limits are Salesforce trivia with no
literal C# meaning, but the underlying purpose (bound an otherwise-unbounded
recursive graft) still holds, so they were ported as-is rather than invented
away.

One integration test file (`EnrichmentIntegrationTest`), matching this
session's established pattern for a freshly-landed subsystem (see
`SharedAncestorIntegrationTest`) rather than porting each of Apex's eight
`enrichment/` unit-test files 1:1. 106/106 tests passing.

**Same session, `providers/` (the demo-domain breadth) and a real staleness
bug fixed along the way.** Ported `XFTY_DefaultSObjectProviderLookup` as
`Demo.DefaultProviderLookup` - the starter-kit `IProviderLookup` bundling the
demo Providers - minus its User entry: `XFTY_DefaultUserDataProvider` needs a
live Salesforce org (SOQL for Profile/UserRole, a real DML insert to seed an
admin user at class-load time) that has no C# analog at all in this port, so
it was not attempted, not faked.

Enriched `AccountDataProvider`/`ContactDataProvider` to match Apex's actual
`XFTY_DefaultAccountDataProvider`/`XFTY_DefaultContactDataProvider` field
sets and literal default values (Industry, ShippingStreet/City/Country, Type
for Account; Email, FirstName, a Description on the required Account
relationship for Contact) - they'd been left deliberately minimal earlier in
the port for bootstrapping and were never brought up to parity. Added the
`ShippingStreet`/`ShippingCountry` properties `Account` needed to carry them.

**2026-09-05, evening: repo cleanup + a real performance suite, after Brian
called out the test-count gap and repo clutter directly.** Removed `.forceignore`,
`config/`, `nimbus.properties`, `sfdx-project.json`, `stubs/` - tracked SFDX-CLI
/ Nimbus-local-Apex-runtime deployment tooling with zero function in a
standalone C# repo (this repo never runs `sf project deploy` or Nimbus).
`force-app/`, `docs/`, `test-support/`, `scripts/` stayed - `force-app/` was
still the active reference source mid-port, and the other three were pending
work, not clutter (see below).

Replaced both GitHub Actions workflows - `ci.yml` ran `sf project deploy
--dry-run` against a CI-authenticated scratch org and `full-suite.yml`
provisioned a real scratch org twice a day, neither of which means anything
here - with one `dotnet build && dotnet test` workflow.

Ported the *spirit* of `test-support/XFTY_LoadTest.cls` (volume tests that
push generation toward the governor limits, run informationally in the
scheduled scratch-org workflow) as `PerformanceTest.cs`: 3 000 primaries
with a required parent, 5 000 primaries held in memory, downward generation
of nested children, and a context-aware value pass at volume. Governor
limits (`Limits.getDmlRows()`/`getCpuTime()`/etc.) have no C# meaning - this
port already dropped `GovernorBudget` for that - so these measure wall-clock
(`Stopwatch`) and a rough allocation figure (`GC.GetTotalMemory`) instead,
against deliberately generous ceilings (order-of-magnitude headroom) so they
catch an accidental O(n²) regression without being a tight budget a slower
CI runner could trip. Tagged `[Trait("Category", "Performance")]` and run as
a separate, `continue-on-error` CI step - not blocking, the same role
`XFTY_Load` plays running only in the scheduled full-suite workflow rather
than on every push.

**Found and fixed while doing that:** `MapBackedLookup.RegisterSharedAncestorDefaults`
still threw `NotSupportedException("Shared ancestors are not ported to C#
yet.")` - correct when it was written, stale ever since `SharedAncestor` was
built earlier this same session. Fixed to actually call
`SharedAncestor.PutIfAbsent(name, template)` per entry, matching Apex's
`XFTY_ProviderLookups`. Covered by a new regression test proving a lookup's
own registered default resolves without the test ever calling
`SharedAncestor.Put` itself. 112/112 tests passing.

**Apex test-suite parity: complete.** A later, much longer stretch ported the
remaining Apex test files folder by folder - `core/`, `engine/`,
`enrichment/`, `relationships/`, `lookup/`, `persistence/`, `values/`,
`providers/` - finishing at 502/502 tests passing, stable across repeated
runs. `seeding/` stays permanently out of scope. `predicates/` turned out to
already be fully covered from earlier work. Along the way: three distinct
classes of `SharedAncestor` cross-test static-state contamination were found
and fixed (an ancestor left registered-but-unresolved by an intentional-throw
test poisons every later test's resolution pre-phase until
`SharedAncestor.Disable(name)` is called on it; `ManualResolutionOnly()` has
no unsetter and is a single process-lifetime flag, so the 3 Apex tests
depending on it were dropped rather than left to intermittently break
unrelated tests depending on run order); a real null-safety regression in
`DeferredInsertBuffer.Collect(Bundle?)` (missing the null-guard Apex's
`primaryRecordsOf(bundle)` helper had) was found and fixed. This is now the
established, documented pattern for working in a shared xUnit process instead
of Apex's per-test-method-reset one - see
[reference/salesforce-considerations](../reference/salesforce-considerations.md)
and [reference/known-issues](../reference/known-issues.md).

**`docs/` (~45 markdown pages): fully ported.** Every page under `docs/use/`,
`docs/extend/`, `docs/reference/`, `docs/contribute/`, and `docs/roadmap/` was
rewritten against the actual C# API surface (verified against source, not
translated blind) rather than left describing `XFTY_DummySObjectProvider`/
`SObjectField`/DML verbatim. Real, previously-undocumented findings that came
out of doing this carefully:

- `InsertMode.Now` always throwing means `.DepthBatched()` currently has *no
  observable effect at all* through `RecordProvider`'s public API - it only
  engages when combined with `Now`, which throws either way. The only way to
  exercise the depth-batching algorithm today is the lower-level
  `DepthBatchedInserter.ResolveAll(records, links, InsertMode.Mock)` call
  directly. Documented rather than left to look like working, tested
  behavior it isn't.
- `SObjectInjector` needed none of Apex's `XFTY_BlobCarrier`/compound-field-
  map/polymorphic-relationship-name machinery - reflection sets any property
  directly, so there's no JSON round-trip to protect a `Blob` field through in
  the first place. A genuine simplification, not a gap.
- Static-state lifetime is the single biggest "coming from Apex" surprise and
  now has its own reference page
  ([reference/salesforce-considerations](../reference/salesforce-considerations.md),
  kept at that filename for link stability even though it's no longer really
  about Salesforce): Apex resets every static per test method; a shared xUnit
  process does not, ever, for the life of the process - the opposite risk
  from what Apex's own `@TestSetup` caveat warned about.
- `docs/articles/` (3 personal essays) and the one dated announcement page
  were deliberately left unconverted (with a one-paragraph note explaining
  why) rather than rewritten - they're first-person history/opinion about the
  author's own Apex career, not API documentation a mechanical port could
  honestly touch.

Not done at that point: `scripts/verify-doc-examples.py`'s C# equivalent and
an `examples/`-style runnable-doc-test suite to check it against - both still
open as of this writing (see [coverage-standards](coverage-standards.md)).

---

## 2026-09-10: `SimpleRecordProvider<TRecord>` removed - it was an invented base class

Brian caught an `abstract class SimpleRecordProvider<TRecord>` that had crept
into `Xfty/Core/RecordProviders/` and told me, in no uncertain terms, to get
rid of it and make every Provider that used it compose a template instead of
inheriting.

He's right. Apex has no such base. Its own example tests each declare a
`private abstract class BaseProvider implements XFTY_DummySobjectProviderIntf`
*locally, inside one test file* when they want to share the three-line
delegation across a handful of fixture Providers - that's a test-file
convenience, not a library type, and the C# port already keeps those
file-local (`MultiVariantProviderTest`, `RecordFactoryTest`). Promoting the
idea to a shipped `Xfty.Core` base class was a structural invention with no
original to point at - the same mistake as the earlier `FieldPredicateBase`
(see 2026-09-05).

**What changed:**
- `SimpleRecordProvider<TRecord>` deleted.
- Every Provider that extended it - `BlankCaseProvider`, `CaseWithAccountProvider`,
  the various `LeafUserProvider` / `AccountWithOwnerProvider` fixtures across
  `Xfty.Test`, and the demo Providers in the `Bogus` / `VectorDatabases*` /
  `EntityFrameworkCore` test projects - now implements `IRecordProvider`
  directly: a `private MasterTemplate _template { get; }`, `PrimaryTargetField`
  and `MasterTemplate` returning it, and `CreateBundle` delegating to
  `RecordFactory.CreateBundle`. This is exactly the shape the bundled
  `ContactDataProvider` / `AccountDataProvider` already had, so it's a
  consistency win, not a new pattern.
- The Provider layer now has no inheritance at all - every Provider is a leaf
  class implementing the interface.

`dotnet build`: 0 warnings / 0 errors. Full suite green (`Xfty.Test` 582,
plus the add-on test projects). `verify-doc-examples.py` / `verify-doc-links.py`
still pass - no doc example ever referenced the base class.

---

## 2026-09-10: value types, positional records, value-type fields, pluggable mock Ids, and the "Id" name assumption

Brian raised a design thought: XFTY is built for classes and record classes,
and `struct` was never really considered - but since the keyword `record` has
nothing to do with database records (it's Pascal's, a linguistic
coincidence), does the work already done for otherwise-immutable records give
`struct` support for free?

Traced the engine, wrote `ValueTypeRecordSupportTest` to replace reasoning
with a test run, and found two real gaps - one wider than expected:

1. **A non-nullable value-type field never received its configured value.**
   Every value pass gated filling on `field.GetValue(record) is not null`,
   which is always true for a boxed `0` / `false` / empty `Guid` /
   `default(DateTime)` / a zero enum. So a configured plain default, a
   context-aware value, and a wired FK were all silently skipped for an
   `int` / `bool` / `Guid` / `enum` / `DateTime` field. **Never
   struct-specific** - it bit any such field on a class or `record` too;
   `struct` just made it pervasive. Caught because the first test asserted a
   `double` field's Provider default and got `0`.
2. **Positional records didn't generate at all.** A positional `record class`
   (`record Contact(string Id, string Name)`) has no parameterless
   constructor, so `Activator.CreateInstance` threw `MissingMethodException`
   the moment XFTY tried to build a blank template or clone one.

Brian's call was unambiguous: **fix both.** "We shouldn't assume anything
about how consumers are creating their database records" - a consumer may be
handed a positional record or a value type by a third-party library and can't
reshape it, and XFTY's job is to facilitate testing whatever design exists,
not to drive design. The Apex original's own constraints (SObjects are always
mutable reference types with string Ids) are Salesforce's, not .NET's, and a
C# consumer shouldn't inherit them.

**What changed:**

- **`Engine/FieldState.IsUnset`** - one helper, comparing the current value
  against what a freshly-built instance holds (`null` for a reference type or
  `Nullable<T>`, the type default for a non-nullable value type). Replaces the
  `is not null` check in `PlainValueFiller`, `ContextAwareValuePass`,
  `DescendantValuePass`, and `LookupWiring`. `NonNullableValueFieldTest`
  covers the int/bool/Guid/enum/DateTime families, the record-class and
  record-struct angles, override-template presets, a context-aware value, and
  a wired FK.
- **`Internal/BlankInstances.Of`** - the public parameterless constructor
  when there is one (running field initializers, as before), an uninitialized
  instance otherwise (`RuntimeHelpers.GetUninitializedObject` on net8.0+,
  `FormatterServices.GetUninitializedObject` on netstandard2.0 - the net472
  `Xfty.NetStandardCompat.Test` exercises that branch). Used everywhere a
  record type is instantiated: `RecordCloneFactory`, `RecordProvider`,
  `SharedAncestorProvider`, `ChildProvider`. `ValueTypeRecordSupportTest`
  covers `record struct`, `readonly record struct`, positional `record
  class`, and positional `record struct`.

**The residual, unchanged and symmetric:** an *override template* can't force
a field to its empty value (`Quantity = 0`, or `Notes = null`) and beat a
Provider default - "set to empty" reads the same as "never set". This always
applied to reference types too; the fix just made value types behave the same
way. Any other value works; the tracked `provider[x => x.Field] = value` or
`RemoveFromMasterTemplate(x => x.Field)` pins the empty value when a test
needs it. `ForcingAnEmptyFieldValueTest` covers both. Documented in
`known-issues.md`.

**Still not done, deliberately:** `InsertMode.Now` against a `struct` primary
(the EF Core gateway tracks class entities), and deep multi-level `Inject(...)`
over a `struct` graph (each graft copies the value). Both are genuine edges of
a shape `record struct` isn't meant for - small value-like data, not
aggregate roots - not a use case to re-architect the engine around.

Same conversation: **`known-issues.md` rewritten** to be lean and
user-facing - current limitations only, no defect backlog, no fixed-bug
history, and the Apex-heritage "no C# analog" list demoted to a short
footnote (few readers arrive wanting to imitate Salesforce). The fixed-bug
narratives and the open doc-verification item moved here (below).

### Then: pluggable mock Ids, and the "Id" property-name assumption

Brian, on the mock-Id path: *"InsertMode.Mock should support absolutely any
id type imaginable"* (built-in support within reason), *"replaceable when
some project has their own id conventions"*, set on the Master Template with
a per-call `RecordProvider` override *"similar to configuring field values,
relationships, etc."*

- **`IMockIdGenerator`** + `MockIdContext` + `DefaultMockIdGenerator`
  (string/int/long/Guid, loud throw otherwise). `MasterTemplate.WithMockIdGenerator`
  carries through `Copy()`; `RecordProvider.SetMockIdGenerator` routes through
  `RecordProviderTemplateConfig` so it lands on this call's template copy
  only - ancestors resolve their own. `RecordFactory.MockIds` picks
  `template.MockIdGenerator ?? DefaultMockIdGenerator.Instance`.

Then Brian caught `IdMocker.AddIds`'s `record.GetType().GetProperty("Id")!`:
*"we shouldn't assume the id field will be called `Id` ... this is exactly
why RecordProvider has `PrimaryTargetField`"* - and asked for a sweep of
other hard-coded assumptions, *"not just in regards to Id mocking"*.

- **The `"Id"` property-name assumption, removed from the engine.** Every
  `Bundle` already carries `PrimaryTargetField`; that is now the source
  everywhere a primary key is read: `RecordProviderChildConfig` (had it in
  scope already), `LookupWiring`'s no-`RelatedField` fallback (parent
  bundle's `PrimaryTargetField`), `InverseAlignment` (new optional
  `parentPrimaryField` param, supplied by `Bundle.PrimariesResolvingTo` and
  `BundleEnricher`), the depth-batched insert path (`DeferredInsertBuffer`
  records an id-field-by-type map from each bundle; `DepthBatchedInserter`
  and `PersistenceGatewayExtensions.InsertMixed` take it), and `SharedAncestor`
  (`GetId`, the resolved single-record bundle - stashes the field at
  resolution from the bundle, or `SharedAncestorProvider.PrimaryField(lookup)`).
  `"Id"` reflection survives only as a last-resort fallback for a hand-built
  caller supplying no map. `NonIdPrimaryKeyTest`.
### Then: the defects that sweep turned up, fixed

- **`InsertMode.Mock` clobbered a preset primary key.** `RecordFactory`'s
  Mock pass set the key unconditionally; the depth-batched path already
  filtered on "unset". Aligned - `RecordFactory.MockIds` now fills only
  `FieldState.IsUnset` keys.
- **Custom `IMockIdGenerator` reached the depth-batched / deferred path.**
  `Bundle` now carries `MockIdGenerator` (set by `RecordFactory` from the
  template); `DeferredInsertBuffer` collects a generator-by-type map next to
  the id-field-by-type one; `DepthBatchedInserter.MockIds` uses it.
- **`SharedAncestor` value-vs-generate moved off the `Id` heuristic where a
  lookup exists.** `Put(name, record)` still checks an `Id`-named property
  (nothing else is available at registration), but `SharedAncestorResolver`
  now decides from `source.PrimaryField(lookup)` - so
  `PutAsTemplate(name, recordWithKeySet)` is used as-is, no sub-graph, no
  re-insert. First cut over-reached (routed `Put` itself through
  `PutAsTemplate`) and broke cycle-breaking + immediate `GetId` for 225
  tests via a static-registry leak - reverted `Put`, kept the resolver-side
  decision.
- **Vector-DB `FindVectorField`** (both preview packages): opaque
  `InvalidOperationException` for zero / silent pick for many `float[]`
  properties → one clear `NotSupportedException`.
- **Still reported, not fixed:** enrichment's `<Name>Id` → `<Name>` /
  `List<TChild>` conventions (a plain FK carries no relationship-name
  metadata). In `known-issues.md`.

`DeepMixedKeyHierarchyTest` is the torture case Brian asked for: ten levels
of required ancestors, keys named `OrderRef` / `LineNo` / `ShipmentKey` /
`CartonId` / `PalletNumber` / `DepotId` / `ManifestId` / `TicketNo` / `Slug`
/ `Uuid`, typed `string`/`long`/`int`/`Guid`, and the string keys carrying a
type prefix, a `SHIP|<unix>|<seq>`, a `MAN-<yyyyMMdd>-<seq>`, a zero-padded
`rgn_000001`, and one built from a value read off the record itself - all
mocked in shape, every FK wired from its parent's real key.
`FlavouredMockIdHierarchyTest` is the follow-up Brian asked for: one POCO,
eight Provider *variants* of it keyed by a discriminator field, an
eight-generation ancestor chain where each generation resolves to a
different variant, and the mock-Id generator picked per resolved Provider
(some `DefaultMockIdGenerator`, some variant-specific) - proving the
generator follows the variant, not the type.

### WSL is the SAC workaround, and it caught a real contamination bug

Brian pointed out the repo has a second test environment: the `Ubuntu` WSL
distro, where SAC (a Windows-only policy) can't block anything. `wsl -d
Ubuntu -e bash -lc "cd /mnt/e/... && dotnet test --solution
Xfty.ci-cross-platform.slnf"` runs the whole cross-platform suite. (The
Linux `dotnet test` CLI wants `--project`/`--solution`, not a bare
directory.) Every suite that had been SAC-blocked passes there - **and so
does `Xfty.FSharpAsync.Test`**, so its Windows MTP "FileNotFoundException"
was environmental too, not a real failure.

WSL also runs test classes in a *different order* than Windows, which
surfaced a contamination bug the Windows ordering had been hiding: the two
new test classes that register a `SharedAncestor` of a `file`-local type
(`DeepMixedKeyHierarchyTest`, `NonIdPrimaryKeyTest`) leaked it into the
process-static registry, and every later test's shared-ancestor pre-phase
then choked resolving a type its own lookup didn't know. The suite's
existing shared-ancestor tests dodge this only by using `Account`/`Contact`,
which every lookup has. Fix: both classes now reset the registry in their
constructor *and* `Dispose` - the same pattern `SharedAncestorResetTest`
already uses. (There is also a pre-existing latent leak - some test leaves
an `Account` shared ancestor unresolved - harmless with `DefaultProviderLookup`,
poison with a custom one; the constructor reset guards against it too.)

`dotnet test` (WSL, full cross-platform `.slnf`): **683 green**, every
project. `Xfty.Test` alone: 627 (582 baseline + 45 new). Windows: same 627
for `Xfty.Test`, plus `Xfty.NetStandardCompat.Test` (net472, Windows-only)
4 green; the SAC-blocked suites are green on WSL. `verify-doc-examples.py` /
`verify-doc-links.py` pass; `check-apex-style.py` clean.

---

## Fixed defects (moved from known-issues.md)

Kept for contributor context - these are resolved and of no interest to
someone evaluating XFTY. The two most recent (the value-type-field and
positional-record fixes) are written up in the dated entry above.

- **`RelatedOnly`'s user-facing docs described the opposite of what it
  actually does - the code was correct, three doc pages were wrong.**
  `docs/use/insert-modes.md`, `getting-started.md`, and
  `reference/api-cheatsheet.md` all described `RelatedOnly` as pure,
  offline Mock-Id generation needing no persistence at all.
  `docs/contribute/architecture.md` had the correct behavior the whole
  time: `GenerationContext.ForRelated()` upgrades `RelatedOnly` to `Now`
  for ancestor generation specifically, by design - confirmed directly
  ("The code is correct... The use case for RelatedOnly is when the
  developer needs/wants uninserted records which relates to existing
  already persisted records"). A primary generated under `RelatedOnly`
  relates to a **real, persisted (or persistable) ancestor** - a mocked
  Id would be a dangling reference to nothing once the caller actually
  inserts the primary itself. Confirmed with a throwaway probe against
  the real engine before touching anything: `Supply()` on a `RelatedOnly`
  Contact with a required Account and no gateway configured throws
  `NotSupportedException`, identical to `Now`. All three doc pages
  corrected; two permanent regression tests added to
  `PersistenceGatewayTest` (the ancestor is genuinely inserted through
  the gateway while the primary stays un-Id'd; the same call throws
  without one) since nothing end-to-end had exercised this path before -
  the one existing `RelatedOnly` test used a Provider with no ancestors
  at all, so it never touched this code.

  **Superseded the same day:** `RelatedOnly`/`MockRelatedOnly` no longer
  exist as `InsertMode` values - the behavior above is unchanged, but it's
  reached via `.ExcludePrimaryIds()`/`.IncludePrimaryIds()` (an orthogonal
  setting on `RecordProvider`, not a mode) plus whichever `InsertMode`
  fits, e.g. `Now` + `.ExcludePrimaryIds()`. See
  [use/insert-modes.md](../use/insert-modes.md#excluding-the-primary---excludeprimaryids).
  Kept this entry for the "why the code is right, not a bug" reasoning,
  which still holds - only the API shape changed.
- `DeferredInsertBuffer.Collect(bundle)` called `bundle.PrimaryRecords()`
  directly with no null-guard, unlike Apex's null-safe
  `primaryRecordsOf(bundle)` helper — `Add(null)` / `InsertGraph(null)` /
  `Flatten(null)` would `NullReferenceException` instead of tolerating `null`
  like Apex does. Fixed with a matching `PrimaryRecordsOf(Bundle?)` helper.
- `RecordProvider.AssertNoRecordTypeConflict`'s exception message did not
  name the offending type.
- A missing null-guard on the shared-ancestor path meant a couple of tests'
  own lookups (missing a `User` provider) failed silently and left an
  unresolved shared ancestor behind, contaminating unrelated later tests —
  see the static-state entry in
  [reference/salesforce-considerations](../reference/salesforce-considerations.md);
  fixed by completing those lookups.
- **`SharedAncestor.ManualResolutionOnly()` was previously untestable in this
  port's own suite** — it has no unsetter of its own, so one test calling it
  would permanently disable the shared-ancestor pre-phase for every test
  running afterward in the same process. `SharedAncestor.ResetAllForTesting()`
  fixes this (it clears the manual-resolution flag along with the registry),
  proven end to end in `SharedAncestorResetTest` - `ManualResolutionOnly()`
  is genuinely exercised there now, not skipped.
- **`SharedAncestor`'s registry could crash under real concurrent access -
  not a theoretical risk, an actually-reproduced one.** `ByName` was a plain
  `Dictionary`, `Disabled` a plain `HashSet`, and `SharedAncestorResolver`'s
  own `_running`/`InProgress` fields were unsynchronized; this port's own
  test suite never hit it only because it explicitly disables xUnit's
  *default* collection parallelism. Building `Xfty.Xunit.Test` without
  that same opt-out surfaced it immediately: `InvalidOperationException`
  from `Dictionary`'s internal state, corrupted by two threads racing to
  mutate it. Fixed - `ByName`/`Disabled` are now `ConcurrentDictionary`s,
  `_manualResolution` is `volatile`, and the actual resolve-and-mutate work
  is serialized through a lock in `SharedAncestorResolver` (every path that
  can trigger resolution funnels through it, so one lock there covers the
  whole subsystem). `SharedAncestorConcurrencyTest` reproduces the original
  crash reliably against the pre-fix code (confirmed by literally reverting
  the fix and re-running it) and passes reliably against the fix - 200
  concurrent attempts, repeated runs, no corruption. Any real consuming
  project that leaves xUnit's default parallelization on - which is most
  xUnit projects, since disabling it is the opt-out - was exposed to this;
  it no longer is.

---

## Open contributor item: doc-example verification for the add-on packages

Moved from `known-issues.md` - a maintainer task, not something a consumer
needs to weigh.

`scripts/verify-doc-examples.py` only ever scanned `Xfty.Test/` for backing
tests, even after add-on packages (`Xfty.AutoFixture`, `Xfty.AutoBogus`,
`Xfty.Bogus`, …) got their own `Xfty.*.Test` projects and their own
`docs/use/*.md` pages — those pages' code blocks were never actually checked
against anything, silently, because none of them carry a `Runnable:` marker.
The scanning gap itself is fixed: `TEST_DIRS` now discovers every `*.Test`
project, not just the core one.

**Still open:** turning that check *on* for `docs/use/autofixture.md` and
`docs/use/autobogus.md` (adding their `Runnable:` line) currently fails —
their examples are genuinely backed by real tests
(`XftyCustomizationTest`/`AutoFixtureUnsetFieldFillerTest`,
`XftyAutoBogusTest`/`AutoBogusUnsetFieldFillerTest`), but small, real drift
has crept in between the doc prose and the test code since they were last
hand-verified: the docs' placeholder variable is `lookup`, the tests call a
`Lookup()` helper method instead, and the docs' `CreateMany<Contact>(3)`
example has no `Contact` counterpart in either test file (only `Account` is
covered). None of this means the *behavior* is wrong — both pages'
philosophy and API shape were traced by hand against the real source while
writing each package's own nuget.org README — but closing it properly means
either renaming to a shared `lookup` local at the relevant call sites (this
port's own established convention — see any core `docs/use` page's Runnable
tests) or adding the missing `Contact` coverage, not just adding the marker
and letting it fail.
