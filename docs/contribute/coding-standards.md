# Coding Standards

The rules this port's code is held to. They apply to anyone changing this
repo — human or AI. When a change is reviewed, this is the checklist.

**The authoritative style rules live in two files at the repo root, not
here:**

- **`.editorconfig`** — `EnforceCodeStyleInBuild=true`, so `dotnet build`
  itself fails a violation. Notably: IDE0058 (discard unused fluent-return
  values with `_ =`), IDE0032 (private fields as `{ get; }` properties, not
  plain fields), IDE0022 (expression-bodied methods where possible), IDE0090
  (`new()` target-typed construction).
- **`CSharp Style Rules.txt`** — the project's own house rules, layered on top
  of the analyzers: prefer functional over imperative (no `for`/`while`),
  explicit over implicit, methods under 10 lines, one expression per line, no
  nesting past two blocks, no unnamed magic numbers/strings, ternary over
  if/else (never on one line), classes under 100 lines, **no nested/inner
  classes** (use C#'s `file` access modifier for a helper scoped to one file
  instead), no circular dependencies.

This page covers what those two don't: naming/design principles carried over
from the Apex original, and this port's own testing conventions.

---

## Design

- **Polymorphism over branching.** A `null`/type check that the same code
  makes in more than one place is a missing type. Introduce a strategy
  interface with one implementation per case; the caller stops choosing. A
  wall of near-identical `if (bad) throw` guards collapses the same way — one
  `Assert...`/reject helper, one line per rule.
- **Flyweight whenever possible.** Interned instances obtained through a
  `Get(...)` factory (see `LookupKey.Get`, `SharedAncestor.Get`), never `new`.
- **Explicit over stateful.** Reject registry / mutable-builder APIs where a
  complete, explicit `Dictionary` plus a stateless static class will do (see
  `ProviderLookups`). Where a collaborator needs values it does not yet all
  have, pseudo-closure the ones it has via the constructor; where it needs many
  things at once, a fluent builder is acceptable.
- **Immutability.** Clone aggressively (`RecordCloneFactory`); derive a new
  object rather than mutating (`MasterTemplate.Copy()`, `GenerationContext`'s
  `With*` methods).
- Remove dead code rather than working around it.
- **No nested classes, ever.** A private helper scoped to one file (a test
  double, a small worker class) is `file sealed class Foo` at namespace scope
  in the same `.cs` file — C#'s direct equivalent of Apex's private-inner-
  class-scoped-to-one-`.cls`-file pattern.

---

## C# gotchas specific to this port

- **`init`-only properties** are read-only after construction by design
  (compile-time only) — `PropertyInfo.SetValue` bypasses that restriction via
  reflection, which `IdMocker`, `RecordCloneFactory`, and `RecordInjector`
  all rely on deliberately. Don't "fix" a reflection-based writer to respect
  `init` — that would break the mechanism.
- **`PropertyInfo` equality** across two `Field.Of<T>(nameof(...))` calls for
  the same property is reference-equal (reflection caches `PropertyInfo`
  instances per type), so it works as a dictionary key without a custom
  comparer — but a `PropertyInfo` obtained through a *different* route
  (e.g. `GetType().GetProperty(...)` vs. `typeof(T).GetProperty(...)`) may not
  be, so this port is consistent about always going through `Field.Of<T>`.
- **`static` state does not reset between xUnit test methods** the way it did
  between Apex test methods — see
  [reference/salesforce-considerations](../reference/salesforce-considerations.md).
  This is the single most important behavioral difference to keep in mind
  while writing tests, and it has caused real cross-test-contamination bugs
  during this port's development (see
  [reference/known-issues](../reference/known-issues.md)).

---

## Testing and coverage

- **Line coverage ~100%**, measured with `coverlet.collector` (see
  [local-development](local-development.md#measuring-coverage)).
- **Branch coverage is the real goal** — every guard, `switch`, and ternary,
  both sides, checked by hand.
- **The framework must never make a consumer debug it.** Any error that could
  trace back to XFTY is loud: a clear `XftyConfigurationException` naming the
  misconfiguration and the fix — never a silent `null` or an opaque downstream
  exception. Accessors that can miss throw at the call site.
- **One test class per unit under test**, sitting beside it under the mirrored
  folder structure in `Xfty.Test/` — `Xfty/Core/Bundle.cs` → `Xfty.Test/Core/BundleTest.cs`.
  Split a class that mixes fundamentally different scenarios (e.g. a fluent-API
  affordance test vs. an end-to-end scenario test).
- **One behaviour per test method.** A positive and a negative case are two
  behaviours — two methods. Every assertion must be about the single value
  captured in the Act; an assertion that re-invokes the code under test (with
  other inputs) is a second Act in disguise.
- **The Act is exactly one statement.** Declare the result variable in Arrange,
  assign it in Act, read it in Assert. Nothing acts in Assert.
- **AAA comments, verbatim**: `// Arrange`, `// Act`, `// Assert`, and
  `// Sanity Check` (a pre-Act assertion that the arranged state is what the
  test assumes) — carried over unchanged from the Apex original's convention.
- **Names:** `<MethodUnderTest>_When<Condition>_<ExpectedOutcome>` — PascalCase,
  no `Test` prefix — e.g. `IsSatisfiedBy_WhenTheFieldIsBlank_ReturnsFalse`,
  `Of_WhenTheListIsNull_Throws`. For an end-to-end / scenario test,
  `<MethodUnderTest>` is the entry point exercised (`Supply_…`,
  `SupplyBundle_…`, `Flush_…`).
- **`[Theory]` for data-row variations** where xUnit's parameterisation fits;
  otherwise a thin `[Fact]` calling a shared private runner that holds the
  `// Arrange` / `// Act` / `// Assert` is the direct equivalent of the Apex
  pattern of one data-row test method per case plus a shared runner.
- **`Assert.*`, never a bare boolean check standing in for one.** Expecting a
  throw: `Assert.Throws<TheSpecificException>(() => act())` — the *exact*
  type, never a bare `Exception`.
- **Test doubles are code too.** Don't paste near-identical
  `IRecordProvider`/`IProviderLookup` implementations across test files — a
  `file sealed class` fixture per file is fine, but reuse a shared helper
  method for anything reused within one file.
