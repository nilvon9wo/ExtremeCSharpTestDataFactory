# Insert Modes

XFTY separates **generating** records from **inserting** them. Insert mode
controls what happens after generation; [inclusivity](relationships.md#inclusivity)
controls how much of the graph is generated. The two are independent.

```csharp
.SetInsertMode(InsertMode.Mock)
```

> `InsertMode.Now` inserts every generated record for real, through an
> `IPersistenceGateway` you supply with `.SetPersistenceGateway(...)`
> (`Xfty.EntityFrameworkCore` ships one backed by EF Core - see
> [PersistenceGatewayTest](../../Xfty.Test/Persistence/PersistenceGatewayTest.cs)
> for the proof, including a real SQLite/Postgres round trip). With no gateway
> configured, `Now` throws `NotSupportedException` rather than silently
> inserting nothing. `Mock` remains the practical default for a unit test that
> only needs a well-formed, Id'd graph and no real database.

---

## The modes

| Mode | Behaviour |
|------|-----------|
| `Never` | Generate records without Ids. |
| `Mock` | Generate realistic-looking Ids **without any persistence**. |
| `RelatedOnly` | Mock-Id only the generated related records; leave the primary records Id-less. |
| `Now` | Insert every generated record through the configured `IPersistenceGateway`. **Throws `NotSupportedException` if none is configured.** |
| `Later` | Behaves exactly like `Never`; documents that the caller will insert later. |
| `Deferred` | Generate like `Never`, but register every record so one flush handles the whole set — see [deferred-insert](deferred-insert.md). Flushing also needs a configured gateway, same as `Now`. |

The generated data is identical regardless of mode; only persistence changes.

---

## `Mock` — the default for a unit test

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
optional related records, then inserts them through
`.SetPersistenceGateway(gateway)`:

<!-- sketch -->
```csharp
Contact result = (Contact)new RecordProvider(typeof(Contact), lookup)
    .SetPersistenceGateway(new EfPersistenceGateway(dbContext))
    .SetInsertMode(InsertMode.Now)
    .Supply();
```

`IPersistenceGateway` is a one-method seam
(`Insert(List<object> records, PropertyInfo idField)`), so it works with EF
Core, Dapper, raw ADO.NET, or a hand-rolled fake in a test - `Xfty.Test`
proves the mechanism against an `NSubstitute` mock, and
`Xfty.EntityFrameworkCore.Test` proves it against a real SQLite database and
(when Docker is available) a real Postgres container. With no gateway
configured, `Now` throws `NotSupportedException` instead of silently
inserting nothing.

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
`Now` + child `Mock`) throws before generation reaches the persistence step -
a mock Id and a real inserted row can never coexist correctly in the same
graph. Under `Deferred` / `.DepthBatched()` a child's override is
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
| A persistence gateway is configured and rows should actually be saved | `Now` |

Start with `Mock` + `Required` inclusivity — realistic Ids, valid required data,
compact graphs, no dependency on a database. See
[advanced/unit-vs-integration](advanced/unit-vs-integration.md).

See also: [deferred-insert](deferred-insert.md) · [relationships](relationships.md) · [bundles](bundles.md)

Runnable: `RecordFactoryTest`, `RecordProviderIntegrationTest`, `PersistenceGatewayTest`
