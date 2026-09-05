# Keeping Large Graphs Manageable

When a test needs a lot of data, two levers keep the graph small and fast.

---

## 1. Generate less — inclusivity

Prefer [`Required`](../relationships.md#inclusivity) over `All`. Every optional
relationship can itself generate more relationships.

## 2. Stop the recursion — `PreventCascade`

For deep or circular models, [`PreventCascade`](../relationships.md#preventcascade)
generates the first level of relationships and no further.

> Apex's third lever, `.DepthBatched()` (collapsing many `insert` statements
> into one per dependency depth), has no observable effect in this port yet —
> it only changes behaviour when combined with `InsertMode.Now`, which always
> throws here. See [deferred-insert](../deferred-insert.md) and
> [reference/known-issues.md](../../reference/known-issues.md).

---

## Measuring

`PerformanceTest.cs` (tagged `Category=Performance`, run as its own CI step)
measures wall-clock time and rough memory allocation for large generations —
3,000 primaries with a required parent, 5,000 primaries held in memory, nested
child generation, and a context-aware value pass at volume — against
deliberately generous ceilings. Apex's governor-limit warnings
(`Limits.getCpuTime()` / `getDmlRows()` / etc.) have no C# meaning and are not
ported; see [reference/volume-and-limits.md](../../reference/volume-and-limits.md)
for what this port measures instead. Model your own volume assertions on
`PerformanceTest.cs`.

Runnable: `PerformanceTest`
