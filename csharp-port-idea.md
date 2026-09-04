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
- Test stack: `xunit` + `xunit.runner.visualstudio` + `FluentAssertions` +
  `NSubstitute` + `coverlet.collector` + `Microsoft.NET.Test.Sdk`.
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
uses, FluentAssertions `.Should()` instead of `Assert.areEqual`, `[Theory]`/
`[InlineData]` instead of a runner called from thin `@IsTest` data-row
methods). The one piece kept **literal**, not just in spirit: the AAA
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
