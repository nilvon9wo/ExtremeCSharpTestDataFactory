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
- `SharedAncestor.ManualResolutionOnly()` **has no unsetter, in Apex or
  here** — Apex tolerated that because its statics reset between methods
  anyway; in a shared xUnit process, one test calling it permanently disables
  the shared-ancestor pre-phase for every test that runs afterward in the same
  process, until the process restarts. There is currently no safe way to test
  `ManualResolutionOnly()` in this port's own test suite for exactly this
  reason — see [known-issues](known-issues.md).

**What this means for your own tests:**

- Give every test's shared-ancestor names something unique to that test (a
  GUID suffix, the test method name) rather than a short literal like `"hq"` —
  a name collision with an unrelated test is a real, silent risk in a shared
  process.
- If a test deliberately leaves a shared ancestor unresolved (to prove a
  null-FK scenario, say), call `SharedAncestor.Disable(name)` afterward so it
  doesn't linger.
- Do not call `SharedAncestor.ManualResolutionOnly()` in a test suite unless
  you are certain nothing else in the same test run depends on the
  auto-resolution pre-phase — there is no way to turn it back off.
- The same caution applies to any `static` state your own value expressions
  keep (an incrementing counter, say) — it is *shared across the whole test
  run*, not reset per test method the way it would be in Apex. That is
  generally fine (it is what keeps `UniqueStringExpression` genuinely unique
  across your whole suite), but do not assume a fresh start per test method.

---

# `@TestSetup` — Not Applicable

Apex's `@TestSetup` mechanism, and its specific conflict with Apex's
per-method static reset, does not exist in C#/xUnit at all — there is no
analogous "runs once, rolled back with the rest of the method's DML"
mechanism, because there is no persistence layer yet (see
[insert-modes](../use/insert-modes.md)) and no such thing as `@TestSetup` in
.NET.

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
Types — has no equivalent here, because this port has no persistence layer to
run any of that against yet. If a future EF (or similar) backing is added,
its own validation/constraint behaviour would deserve the same treatment
Apex's did: keep that knowledge centralized in the Provider, not scattered
across individual tests.

---

# Summary

The one thing worth remembering from this page: **Apex resets statics between
test methods; this port's test process does not.** Every recommendation above
follows from that single inversion.
