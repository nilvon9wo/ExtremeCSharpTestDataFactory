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
  and do not reset between test methods**, unlike Apex, where every static
  resets automatically per test method. A shared ancestor left registered but
  unresolved (an intentional-throw test, say) poisons every later test's
  shared-ancestor pre-phase; the fix is `SharedAncestor.Disable(name)`
  immediately afterward. See
  [reference/salesforce-considerations](salesforce-considerations.md) for the
  full explanation and the naming/cleanup convention this port's own test
  suite now follows throughout.
- **`SharedAncestor.ManualResolutionOnly()` has no unsetter, in Apex or
  here** — and here, unlike Apex, it is a single flag for the entire test
  process, not per test method. There is no way to safely exercise it in this
  port's own shared-process test suite; the corresponding Apex tests were
  deliberately not ported (dropped with an explanatory comment) rather than
  left to intermittently break unrelated tests depending on run order.

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
