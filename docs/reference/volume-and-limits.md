# Volume & Performance

## Do you need this page?

**Almost certainly not for a normal test.** A typical test generates 1–20
records; a "bulk" test 100–200. `Xfty.Test/PerformanceTest.cs`'s ceilings are
in the **thousands**. You would have to be deliberately building a large
fixture to get close. If that is you, read on.

"Primary records" here means the records you asked for — one per
`SetQuantityPerTemplate(n)`, `SupplyList()` returns them. Each primary may pull
in a graph of generated parents, so *records generated* is usually a small
multiple of *primaries*.

---

Salesforce imposes hard per-transaction governor limits (DML rows, SOQL
queries, CPU time, heap) that Apex's `XFTY_GovernorBudget` warned about as
generation approached them. **None of that exists in this port** — there is no
transaction, no DML, no governor limits to spend, because there is no
persistence layer yet (see [insert-modes](../use/insert-modes.md)).
`GovernorBudget` was not ported for exactly that reason; see
[known-issues](known-issues.md).

What this port measures instead is what actually matters for a .NET test
process: **wall-clock time and memory allocation**, via
`System.Diagnostics.Stopwatch` and `GC.GetTotalMemory`, in
`Xfty.Test/PerformanceTest.cs`. It runs tagged
`[Trait("Category", "Performance")]`, generously bounded (an order of magnitude
above what a healthy run takes locally) so it stays green on a loaded CI
runner — it exists to catch an accidental O(n²) regression, not to enforce a
tight budget.

---

## What it measures today

| Scenario | Ceiling |
|----------|---------|
| `Mock`, 3,000 primaries with a required parent (6,000 records total) | well under 5 seconds |
| `Never`, 5,000 primaries with a generated parent each, held in memory | well under 512 MB allocated |
| `Mock`, 15 Accounts × 10 `WithChildren` Contacts each (165 records) | well under 2 seconds |
| `Mock`, 3,000 primaries each with two context-aware value expressions (a sibling copy + a custom `IContextAwareExpression`) | well under 5 seconds — the value pass stays cheap at volume |

There is no equivalent yet for a `Now`-mode insert-count scenario, a
`.DepthBatched()` DML-statement-count scenario, org seeding, or
`Inject`/`InjectAll` at volume — Apex's original load suite covered those, but
each depends on a persistence layer this port does not have. Once one exists,
this page (and `PerformanceTest.cs`) should grow the equivalent cases.

---

## Keeping generation cheap

- Prefer **`Mock`** — it does no work beyond assigning an Id.
- Use **`Required`** inclusivity, not `All`; use **`PreventCascade`** for deep
  or circular models.
- Generate the **minimum row count** the test needs.
- For a shared parent across many children, use a
  [shared ancestor](../use/shared-ancestors.md) — it is generated once, not
  once per child.

See [use/advanced/large-graphs](../use/advanced/large-graphs.md) for the
levers in more detail.
