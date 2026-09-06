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

## Doc-verification gap

`scripts/verify-doc-examples.py` only ever scanned `Xfty.Test/` for backing
tests, even after add-on packages (`Xfty.AutoFixture`, `Xfty.AutoBogus`,
`Xfty.Bogus`, …) got their own `Xfty.*.Test` projects and their own
`docs/use/*.md` pages — those pages' code blocks were never actually checked
against anything, silently, because none of them carry a `Runnable:` marker.
Fixed the scanning gap itself: `TEST_DIRS` now discovers every `*.Test`
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
and letting it fail. Tracked here rather than done as a drive-by while
fixing something unrelated.

---

## Fixed (kept for context)

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
