---
name: csharp-port-idea
description: "discussion of porting XFTY concepts to C#/.NET as a portfolio + practical showcase"
metadata: 
  node_type: memory
  type: project
  originSessionId: a085fbeb-e844-4c0d-b85f-eaab06462005
  modified: 2026-09-04T16:07:30.292Z
---

Brian raised (post `b24f389`, conversation lost to a closed window, recounted
2026-09-04): whether it's viable and worth the effort to port XFTY to C#.
Motivation is dual — showcase his C# ability, and XFTY has real features
AutoFixture and similar .NET test-data libraries don't offer, notably
**context-aware values** (see [[org-seeding-branch]] / beta's
`GenerationContext` work).

Points agreed so far (no build started, this is still at the "should we"
stage):
- **Drop the serialize/deserialize trick.** That's an Apex workaround (see
  [[serialization-enrichment-branch]] territory) — C# has real reflection, so
  a port would build/inspect object graphs directly instead.
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
this memory file is the handoff since neither conversation state nor
project-scoped memory carries over automatically to a new working directory).

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
- `seeding/` (the `@IntegrationTest` DML-without-rollback hack, see
  [[org-seeding-branch]]) — **true dead end, zero C# analog.** C# unit tests
  just call `SaveChanges`/persist normally, no special hack needed.
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

**Conventions to match** (found by inspecting `E:\projects\CSharp\Skroob5000`
as a template — Brian later clarified (2026-09-04) it's *not* confirmed to be
his most recent/best-configured C# project, maybe Certara is, he wasn't sure
and said it isn't important; treat Skroob5000's setup below as one reasonable
precedent, not gospel):
- `net10.0`, `ImplicitUsings`/`Nullable` enabled.
- Test stack: `xunit` + `xunit.runner.visualstudio` + `NSubstitute` +
  `coverlet.collector` + `Microsoft.NET.Test.Sdk`. **Not** FluentAssertions,
  despite matching Skroob5000 - Brian caught (2026-09-04) that FluentAssertions
  8.x is Xceed-licensed, free for non-commercial use only, and didn't want a
  license-encumbered dependency for something as trivial as an assertion,
  especially one potential users of a portfolio library would shy away from.
  Plain xUnit `Assert.*` instead - no extra dependency, and it happens to
  read closer to Apex's own `Assert.*` calls than a fluent chain would.
- `LanguageExt.Core` in the main project — matches
  `E:\projects\CSharp\CSharp Style Rules.txt` rule 1 ("never use for/while,
  prefer functional") which plain C# can't really honor for stateful code
  without a library like this (`Option`/`Either` etc.).
- Project split: `<Name>.Core` + `<Name>.Core.Test`, `RootNamespace`/
  `AssemblyName` both set explicitly, `ProjectReference` wiring, `<Using
  Include="Xunit" />` global usings block.
- Strict `.editorconfig` at repo root encoding the style rules as analyzer
  errors (`dotnet_style_qualification_for_*=true:error`,
  `dotnet_style_parentheses_*:error`, etc.) — copy `Skroob5000/.editorconfig`
  as the starting point.
- Full style rules doc: `E:\projects\CSharp\CSharp Style Rules.txt` (matches
  [[feedback-code-style]] closely: one-expr-per-line, verbs/nouns, no inner
  classes, ≤100-line classes, ≤10-line methods, never nest >2 deep).

**2026-09-04 (scaffolding session): decisions resolved.**
- Root namespace: **`Net.Nowhereatall.Xfty`** (Skroob5000's placeholder-domain
  pattern, keeps the short "XFTY" branding rather than the long folder name).
  Solution/projects are `Xfty.slnx`, `Xfty.Core`, `Xfty.Core.Test`.
- Demo domain for `providers/`-equivalent showcase code: **Contact/Account**,
  mirroring the Apex standard objects 1:1 for clean before/after comparison.
- Repo strategy: **entirely new git repo, not a branch off the Apex history.**
  The Apex checkout's `.git` was deleted and reinitialized fresh in
  `E:\projects\CSharp\ExtremeCSharpTestDataFactory` — no shared history, no
  `origin` pointing at ExtremeApexTestDataFactory. The Apex source tree
  (`force-app/`, `docs/`, etc.) stays in the working tree as the read-only
  reference/scaffold described above; it just no longer carries Apex commit
  history into the new repo.
- Scaffold built and verified this session: `Xfty.slnx` (slnx format, a
  `Solution Items` folder holding `.editorconfig`/style-rules/this file),
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
  Apex workaround unnecessary) for the "loud, named error" `coding-
  standards.md` asks for. Lives at `Net.Nowhereatall.Xfty.Core` root since
  every future module needs it, not just `predicates/`.
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
  `Net.Nowhereatall.Xfty.Core.Predicates` (mirrors the Apex package folder,
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

**Still open when picked back up (superseded by the next entry - the whole
list below got built the same session):** ~~`XFTY_DummySObjectBundle`,
`XFTY_PathValue`/`XFTY_PathTargetValue`, `XFTY_DummySObjectMasterTemplate`,
the `relationships/` interfaces, and eventually `lookup/` and the actual
generation engine.~~

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
`Net.Nowhereatall.Xfty.Core.Core` (the `Core/` subfolder namespace, doubled
by the project name) was pure stutter. `Net.Nowhereatall.Xfty.Core` now
means only the `Core/` subfolder - the project root namespace is
`Net.Nowhereatall.Xfty`. 102/102 tests passing, stable across repeated runs
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
`BreakSoqlLimits()`) - the exact numeric limits are Salesforce trivia with no
literal C# meaning, but the underlying purpose (bound an otherwise-unbounded
recursive graft) still holds, so they were ported as-is rather than invented
away.

One integration test file (`EnrichmentIntegrationTest`), matching this
session's established pattern for a freshly-landed subsystem (see
`SharedAncestorIntegrationTest`) rather than porting each of Apex's eight
`enrichment/` unit-test files 1:1. 106/106 tests passing.
