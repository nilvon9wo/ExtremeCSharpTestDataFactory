# Coming From Salesforce — What Changes

This page is for a reader who knows the Apex original. It covers what carries
over from Salesforce's platform constraints, and — the one genuinely
**surprising** difference, worth reading this page for — how static state
behaves in the *opposite* way here from how it behaved in Apex.

---

# Static State: xUnit Does Not Reset It Between Test Methods

**This is the single most important difference from Apex, and it runs the
opposite direction from the `@TestSetup` caveat this page used to be about.**

Salesforce resets every static variable at the start of each test method — so
in Apex, a `static` field used for cross-record bookkeeping (an incrementing
counter, a set of used unique values, a shared-ancestor registry) was
automatically fresh per test method, and the *only* trap was `@TestSetup`
running before that reset.

**.NET has no such reset.** A `static` field lives for the lifetime of the test
process — potentially the entire `dotnet test` run, across every test class.
This port's static registries (`SharedAncestor`, `DeferredInserter`) behave
exactly like any other .NET static: what one test registers is visible to
every test that runs after it in the same process, unless something explicitly
clears it.

This is not a hypothetical risk — it produced real, confirmed cross-test
contamination during this port's own development:

- A `SharedAncestor` left **registered but unresolved** on purpose (to prove an
  expected-throw scenario) stayed that way for the rest of the process,
  poisoning every later test's shared-ancestor pre-phase resolution with
  `LookupException`s for names that test never registered. The fix:
  `SharedAncestor.Disable(name)` immediately after any test that deliberately
  leaves an ancestor unresolved.
- `SharedAncestor.ManualResolutionOnly()` **has no unsetter of its own, in
  Apex or here** — Apex tolerated that because its statics reset between
  methods anyway; in a shared xUnit process, one test calling it would
  permanently disable the shared-ancestor pre-phase for every test that
  runs afterward in the same process, until the process restarts, *unless*
  something resets it. `SharedAncestor.ResetAllForTesting()` is that
  something (below).

**`SharedAncestor.ResetAllForTesting()` — a real, verified reset, but an
opt-in one:**

Clears the shared-ancestor registry, every `Disable`d name, and the
manual-resolution flag in one call. Call it from your own test suite's
per-test setup — a base test class's constructor, or an xUnit fixture's
`Dispose` — since xUnit creates a fresh instance of the test class (and
runs the constructor) for every test method by default. This is genuinely
the closest thing to Apex's automatic per-method reset available here, but
it is **not** automatic the way Apex's is: nothing in .NET gives XFTY a
hook to call it for you, so a test suite that never wires it up gets no
reset at all. `SharedAncestorResetTest` in this port's own suite proves it
works, including finally exercising `ManualResolutionOnly()` safely - see
[known-issues](known-issues.md).

**Using xUnit? `[IsolatesSharedAncestor]` (separate `Xfty.Xunit` package) is
the same reset, already wired up.** Apply it to a test class or method and
it calls `ResetAllForTesting()` before and after, via xUnit's own
`BeforeAfterTestAttribute` hook - no base class, no fixture, nothing to
remember. `IsolatesSharedAncestorAttributeTest` proves it prevents real
leakage between two test methods that deliberately reuse the same name;
`SharedAncestorLeaksWithoutIsolationTest` proves the leak is real without
it (simulated within one method, since the leak itself depends on no reset
happening between two calls - not something to rely on xUnit's own,
unguaranteed method-ordering to demonstrate reliably across two real test
methods).

**What this means for your own tests:**

- **Either** wire up `SharedAncestor.ResetAllForTesting()` once, in a shared
  base test class or fixture, and rely on it for every test that touches
  `SharedAncestor` — **or** give every test's shared-ancestor names
  something unique to that test (a GUID suffix, the test method name)
  rather than a short literal like `"hq"`, and call
  `SharedAncestor.Disable(name)` after any test that deliberately leaves an
  ancestor unresolved. This port's own test suite uses the second approach
  throughout (see [known-issues](known-issues.md) for why: keeping both
  approaches exercised somewhere), but either is a real, complete answer -
  pick whichever fits your project's existing test-base-class conventions.
- Do not call `SharedAncestor.ManualResolutionOnly()` in a test suite that
  doesn't use `ResetAllForTesting()` unless you are certain nothing else in
  the same test run depends on the auto-resolution pre-phase — without the
  reset, there is still no way to turn it back off mid-run.
- The same caution applies to any `static` state your own value expressions
  keep (an incrementing counter, say) — it is *shared across the whole test
  run*, not reset per test method the way it would be in Apex. That is
  generally fine (it is what keeps `UniqueStringExpression` genuinely unique
  across your whole suite), but do not assume a fresh start per test method.

**A separate concern: concurrent access, not reset timing.** Everything
above is about *when* the registry resets. Whether it can safely be *read
and written from more than one thread at once* is a different question,
with its own history: `SharedAncestor` used to genuinely crash under real
concurrent access (a plain `Dictionary`/`HashSet`, unsynchronized resolver
state) - this port's own suite never hit it only because it disables
xUnit's *default* collection parallelism, which most real xUnit projects
leave on. That's fixed now (concurrent collections, a lock serializing the
actual resolve-and-mutate work) and needs nothing from you - see
[known-issues](known-issues.md) for the fix and the test that reproduces
the original crash against the pre-fix code to prove it.

---

# `@TestSetup` — Not Applicable

Apex's `@TestSetup` mechanism, and its specific conflict with Apex's
per-method static reset, does not exist in C#/xUnit at all — there is no
analogous "runs once, rolled back with the rest of the method's DML"
mechanism in .NET, and rollback-per-method has no equivalent regardless of
whether a test happens to use a real persistence gateway (see
[insert-modes](../use/insert-modes.md)).

**The nearest equivalent to Apex's recommended "static test-class fixture"
pattern is the test class's own instance state**, not a `static` field — xUnit
creates a fresh instance of the test class (and therefore runs the
constructor) for every test method by default, which is what actually gives
you "fresh per test method" in this world. See
[advanced/deep-setup-chains](../use/advanced/deep-setup-chains.md) for the
worked pattern.

---

# Platform-Specific Behaviour — Mostly Doesn't Apply

Apex's list of platform behaviours a Provider author has to stay aware of —
Validation Rules, Flows, Apex Triggers, Duplicate Rules, Required Record
Types — has no equivalent here; those are schema-level behaviors of a
Salesforce org specifically, not something any other persistence backend
reproduces. A real backend's own validation/constraint behaviour (an EF
`DbContext`'s model validation, database constraints, and so on) deserves the
same treatment Apex's did: keep that knowledge centralized in the Provider,
not scattered across individual tests.

---

# Summary

The one thing worth remembering from this page: **Apex resets statics between
test methods; this port's test process does not.** Every recommendation above
follows from that single inversion.
