# Known Issues

Defects, rough edges, and capability gaps versus the Apex original. This page
is for **things that are wrong or missing**, not for undecided design — for
plan status see [../roadmap/README.md](../roadmap/README.md).

---

## Capability gaps — deliberately not ported, documented rather than faked

These are genuine Apex/Salesforce features with **no C# equivalent**, dropped
rather than approximated:

- **`InsertMode.Now` always throws `NotSupportedException`.** There is no
  persistence layer (no EF `DbContext`, no database) — see
  [use/insert-modes](../use/insert-modes.md). This is the single biggest
  functional gap: it means `RelatedOnly` never actually mock-Ids in a way
  distinct from `Mock`, `DeferredInserter.Flush()` always throws, and
  `.DepthBatched()` has **no observable effect at all** through
  `RecordProvider`'s public API — it only changes behaviour when combined with
  `Now`, which throws either way. `DepthBatchedInserter.ResolveAll(records,
  links, InsertMode.Mock)` (the lower-level engine call) is the only way to
  exercise the depth-batching algorithm directly today.
- **Salesforce Record Types have no analog.** `XFTY_RecordTypeLookupKey`,
  `XFTY_RecordTypeDataProvider`, `XFTY_RecordTypeMatching`, and the
  override-template `RecordTypeId` auto-detection are not ported. This port's
  variant system, `FlavouredLookupKey`, uses arbitrary predicates instead —
  see [extend/provider-variants](../extend/provider-variants.md).
- **Org seeding is entirely out of scope.** `XFTY_Seeder`, `XFTY_SeedResult`,
  and Apex's `@IntegrationTest`-based preview have no meaning without a real,
  persistent backing store — see [use/org-seeding](../use/org-seeding.md).
- **No test-user helpers.** `XFTY_DefaultUserDataProvider`'s
  `TEST_ADMIN_USER`, `profileIdFor`, `roleIdFor` depend on a live org's
  `Profile`/`UserRole` schema and `System.runAs` — none of which exist in
  .NET. See [use/test-user-helpers](../use/test-user-helpers.md).
- **`XFTY_GovernorBudget` is not ported.** No C# equivalent of
  `Limits.getCpuTime()` / `getDmlRows()` / etc. exists. This port measures
  wall-clock time and allocation instead — see
  [volume-and-limits](volume-and-limits.md).
- **`RecordInjector`'s `Blob`/compound-field/polymorphic-relationship
  machinery is not needed, not dropped.** Apex's version round-tripped
  through JSON specifically because `SObject.put(...)` rejects relationship
  and read-only fields; this port's reflection-based injector sets any
  property directly, so there is nothing to special-case. See
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
