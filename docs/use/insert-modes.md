# Insert Modes

XFTY separates **generating** records from **inserting** them. Insert mode
controls what happens after generation; [inclusivity](relationships.md#inclusivity)
controls how much of the graph is generated. The two are independent.

```csharp
.SetInsertMode(InsertMode.Mock)
```

> **This port has no persistence layer.** Apex's `Now` inserts real rows via
> DML; this port has nothing to insert into (no EF, no database — see
> [reference/salesforce-considerations.md](../reference/salesforce-considerations.md)).
> `InsertMode.Now` is kept in the enum, generates the whole graph exactly like
> Apex would, and then **always throws `NotSupportedException`** at the point
> where Apex would run `insert`. Every other mode works as documented below —
> **`Mock` is this port's practical equivalent of Apex's integration-test `Now`**
> for proving a graph is well-formed.

---

## The modes

| Mode | Behaviour |
|------|-----------|
| `Never` | Generate records without Ids. |
| `Mock` | Generate realistic-looking Ids **without any persistence**. |
| `RelatedOnly` | Mock-Id only the generated related records; leave the primary records Id-less. |
| `Now` | Insert every generated record. **Always throws `NotSupportedException` in this port.** |
| `Later` | Behaves exactly like `Never`; documents that the caller will insert later. |
| `Deferred` | Generate like `Never`, but register every record so one flush handles the whole set — see [deferred-insert](deferred-insert.md). **Flushing to real persistence also throws in this port**; the value here is building and inspecting the whole graph in memory. |

The generated data is identical regardless of mode; only persistence changes.

---

## `Mock` — the default for this port

```csharp
Contact result = (Contact)new RecordProvider(typeof(Contact), lookup)
    .SetInsertMode(InsertMode.Mock)
    .Supply();

Assert.NotNull(result.Id);
```

Realistic-looking Ids, no persistence layer touched. **Never treat a `Mock`
record as if it were saved** — those Ids do not point at anything real.

---

## `Now`

Generates the requested records, required related records, and (under `All`)
optional related records exactly as Apex would — and then throws
`NotSupportedException` where Apex would insert. Kept in the API (rather than
removed) so a faithfully-ported test can assert on the exact failure point, and
so a future persistence layer (e.g. an EF `DbContext`) has an obvious place to
plug in. See [roadmap/README.md](../roadmap/README.md).

---

## `RelatedOnly`

Mock-Ids the generated parents but leaves the primary records untouched — a test
that needs valid lookup targets but wants to handle the primaries itself.
Internally XFTY upgrades relationship generation while leaving the primaries
alone.

It only affects a Provider's **ancestors**. Child collections
([`With` / `WithChildren`](child-records.md)) are not ancestors, so under
`RelatedOnly` they are generated but not Id'd.

---

## Child collections

A child collection ([`With` / `WithChildren`](child-records.md)) inherits the
parent Provider's mode unless it sets its own. A child may raise or lower that
mode — parent `Never` + child `Mock` is common — with **one** exception: mixing
mock Ids with `Now` in either direction (parent `Mock` + child `Now`, or parent
`Now` + child `Mock`) throws before generation even reaches the (always-failing)
persistence step. Under `Deferred` / `.DepthBatched()` a child's override is
ignored entirely; the whole subtree is generated together.

---

## Choosing

| Scenario | Mode |
|----------|------|
| Unit test (this port's normal case) | `Mock` |
| Testing object construction only | `Never` |
| Test will Id/persist the records itself | `Later` |
| Data built over several calls, one in-memory graph | `Deferred` |
| Needs Id'd lookup targets only | `RelatedOnly` |
| A real persistence layer is wired up | `Now` (not yet possible in this port) |

Start with `Mock` + `Required` inclusivity — realistic Ids, valid required data,
compact graphs, no dependency on a database. See
[advanced/unit-vs-integration](advanced/unit-vs-integration.md).

See also: [deferred-insert](deferred-insert.md) · [relationships](relationships.md) · [bundles](bundles.md)

Runnable: `RecordFactoryTest`, `RecordProviderIntegrationTest`
