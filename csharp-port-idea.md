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

**Conventions to match** (found by inspecting `E:\projects\CSharp\Skroob5000`,
Brian's most recent/best-configured C# project — treat as the template):
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

**Testing conventions carry over from Apex, not just the values/predicates
logic.** `docs/contribute/coding-standards.md` is written in Apex but Brian
confirmed (2026-09-04) it holds "just as true for C#" — treat it as the style
authority for the port generally, not only for the mechanically-portable
classes. Concretely for ported/new xUnit tests: keep the same AAA structural
comments — `// Arrange`, `// Act`, `// Assert`, and `// Sanity Check` (a
pre-Act assertion that arranged state matches what the test assumes) — and
carry over the rest of the "Testing and coverage" section's shape (one test
class per unit under test, one behaviour per test method, the Act is exactly
one statement, `test<Method>_when<Condition>_<expectedOutcome>` naming
translated to C# idiom, parameterised tests as a thin `[Theory]`/data-row
layer over a shared runner holding the AAA comments).
