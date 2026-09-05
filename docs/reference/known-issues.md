# Known Issues

Defects, rough edges, and capability gaps versus the Apex original. This page
is for **things that are wrong or missing**, not for undecided design — for
plan status see [../roadmap/README.md](../roadmap/README.md).

---

## Capability gaps — deliberately not ported, documented rather than faked

These are genuine Apex/Salesforce features with **no C# equivalent**, dropped
rather than approximated:

- **Record-Type-style schema auto-detection has no analog.** Automatically
  inferring a variant from an override template's own discriminator-shaped
  metadata (rather than a field value you name explicitly) needs schema
  description this port has no equivalent of. `DiscriminatorLookupKey` covers
  the actual use case (matching a Provider by a named field's value) over the
  same `FlavouredLookupKey` mechanism as every other variant — see
  [extend/provider-variants](../extend/provider-variants.md).
- **Seeding a long-lived, shared environment is entirely out of scope.**
  Generating and inserting data for the duration of one test run is what this
  library does; leaving a graph behind in a persistent environment for manual
  or downstream use is a different job, deliberately not built here — see
  [use/org-seeding](../use/org-seeding.md).
- **No test-user helpers.** A bundled Provider exposing a ready-made
  admin-equivalent test user, resolved against live role/profile-style
  schema, has no meaning without that schema. See
  [use/test-user-helpers](../use/test-user-helpers.md).
- **No CPU-time/row-count budget tracking.** This port measures wall-clock
  time and allocation instead (see [volume-and-limits](volume-and-limits.md))
  - there's no fixed per-run resource quota to track against in the first
    place.
- **`RecordInjector`'s `Blob`/compound-field/polymorphic-relationship
  machinery is not needed, not dropped.** This port's reflection-based
  injector sets any property directly, including relationship and read-only
  fields, so there is nothing to special-case. See
  [use/record-injector](../use/record-injector.md).
- **`ChildProvider` cannot validate that a relationship field actually
  belongs to the parent type it's hung off**, the way Apex validated via
  schema describe. A misconfigured field surfaces as a wrong or `null` value
  at generation time instead of failing fast at configuration time. See
  [use/child-records](../use/child-records.md).

---

## Real, confirmed risk in a shared xUnit process

- **`SharedAncestor`'s registry and `DeferredInserter`'s buffer are `static`
  and do not reset between test methods automatically**, unlike Apex, where
  every static resets per test method on its own. Three ways to handle it -
  `[IsolatesSharedAncestor]` (separate `Xfty.Xunit` package, resets
  automatically via xUnit's own per-test hook), `SharedAncestor.ResetAllForTesting()`
  (the same reset, wired by hand into your own base test class/fixture), or
  unique names per test (no reset at all) - see
  [use/shared-ancestors.md](../use/shared-ancestors.md) for all three. None
  is automatic the way Apex's reset is; this port's own test suite
  deliberately uses the unique-names approach throughout (see
  [reference/salesforce-considerations](salesforce-considerations.md)) so
  every approach stays genuinely exercised somewhere, not just documented.

---

## Fixed (kept for context)

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
  see "Real, confirmed risk" above; fixed by completing those lookups.
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
