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
| `Now` | Insert every generated record through the configured `IPersistenceGateway`. **Throws `NotSupportedException` if none is configured.** |
| `Later` | Behaves exactly like `Never`; documents that the caller will insert later. |
| `Deferred` | Generate like `Never`, but register every record so one flush handles the whole set — see [deferred-insert](deferred-insert.md). Flushing also needs a configured gateway, same as `Now`. |

The generated data is identical regardless of mode; only persistence changes.
[`.ExcludePrimaryIds()`](#excluding-the-primary---excludeprimaryids) is a
separate, orthogonal setting - not a mode of its own - that composes with
any of the five above.

---

## `Mock` — the default for a unit test

```csharp
Contact result = (Contact)await new RecordProvider(typeof(Contact), lookup)
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
Contact result = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .SetPersistenceGateway(new EfPersistenceGateway(dbContext))
    .SetInsertMode(InsertMode.Now)
    .Supply();
```

`IPersistenceGateway` is a one-method seam
(`Task Insert(List<object> records, PropertyInfo idField)`), so it works with EF
Core, Dapper, raw ADO.NET, or a hand-rolled fake in a test - `Xfty.Test`
proves the mechanism against an `NSubstitute` mock, and
`Xfty.EntityFrameworkCore.Test` proves it against a real SQLite database and
(when Docker is available) a real Postgres container. With no gateway
configured, `Now` throws `NotSupportedException` instead of silently
inserting nothing.

---

## Excluding the primary - `.ExcludePrimaryIds()`

For a not-yet-inserted primary that must still relate to a **real, or
realistically Id'd, ancestor** — an Account that genuinely exists (or will),
not a placeholder Id nothing points at. `.ExcludePrimaryIds()` leaves this
call's own primary record(s) un-Id'd — no Mock Id, no real insert, no
`Deferred` registration for them specifically — while every ancestor they
need is persisted exactly as the configured `InsertMode` already says.
Ancestors are never affected, no matter how deep the chain; only this call's
own top-level output is excluded. `.IncludePrimaryIds()` undoes it, back to
the default.

It composes with any mode, which is the point - each combination answers a
different version of "how does the ancestor need to be real":

**`Now` + `.ExcludePrimaryIds()`** — the ancestor is genuinely inserted,
one at a time as it's generated, through the configured gateway:

```csharp
RecordProvider provider = new RecordProvider(typeof(Contact), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Now)
    .ExcludePrimaryIds()
    .SetPersistenceGateway(gateway);

Bundle bundle = await provider.SupplyBundle();
// bundle's Contact primary is un-Id'd; its Account ancestor is a real, inserted row
```

Throws `NotSupportedException` if no gateway is configured and an ancestor
needs generating - the same requirement bare `Now` has, since nothing about
excluding the primary changes how an ancestor gets persisted.

**`Mock` + `.ExcludePrimaryIds()`** — the same shape, but the ancestor only
needs a mock Id; no gateway required at all:

```csharp
RecordProvider provider = new RecordProvider(typeof(Contact), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Mock)
    .ExcludePrimaryIds();
```

**`Deferred` + `.ExcludePrimaryIds()`** — the capability that needs the
efficient, multi-provider registry: a primary with a deep ancestor tree (or
several separate Providers' worth of ancestors) built and flushed together,
depth-batched, in one real pass, while the primary that relates to it stays
un-Id'd for the whole lifetime of the call:

```csharp
RecordProvider provider = new RecordProvider(typeof(Contact), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Deferred)
    .ExcludePrimaryIds();

Bundle bundle = await provider.SupplyBundle();
await DeferredInserter.Flush(gateway);
// bundle's Contact primary is still un-Id'd after the flush; its Account
// ancestor (and anything else registered before the flush) is really inserted
```

This is the one thing `Now`/`Mock` + `.ExcludePrimaryIds()` alone cannot do:
`Now`'s ancestor insertion is one-at-a-time as each ancestor is generated -
exactly right when insertion order matters (real trigger order, say), but
not batched. Registering under `Deferred` instead lets the whole tree - and
anything else registered before the same flush, across as many other
Providers as needed - resolve together in one depth-batched pass, the
primary excluded throughout.

It only ever affects a Provider's own **primary**. Child collections
([`With` / `WithChildren`](child-records.md)) are not primaries of this
call, so `.ExcludePrimaryIds()` on the parent doesn't touch them - they
still get their own Id under whatever mode they inherit or set, just with a
`null` back-reference if the parent they'd point at was excluded.

**`.IncludePrimaryIds()`** undoes it, back to the default - the last call
wins:

```csharp
RecordProvider provider = new RecordProvider(typeof(Account), lookup)
    .SetInsertMode(InsertMode.Mock)
    .ExcludePrimaryIds()
    .IncludePrimaryIds();

Account result = (Account)await provider.Supply();
Assert.NotNull(result.Id); // back to persisting normally
```

Rarely needed explicitly, since `.IncludePrimaryIds()` is already the
default - but real for a helper method deciding dynamically (a shared
setup routine toggling it based on a parameter, say), or simply for a
caller who would rather state the default outright than lean on it
silently.

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
| Primary relates to a real, already-persisted (or persistable) ancestor, one at a time as it's generated, but the primary itself isn't inserted yet | `Now` + `.ExcludePrimaryIds()` |
| Same, but the ancestor doesn't need to be a real row either - just a valid-looking Id | `Mock` + `.ExcludePrimaryIds()` |
| Same as the `Now` row, but the ancestor tree is deep (or spans several Providers) and needs efficient, batched insertion | `Deferred` + `.ExcludePrimaryIds()` |
| A persistence gateway is configured and rows should actually be saved | `Now` |

Start with `Mock` + `Required` inclusivity — realistic Ids, valid required data,
compact graphs, no dependency on a database. See
[advanced/unit-vs-integration](advanced/unit-vs-integration.md).

See also: [deferred-insert](deferred-insert.md) · [relationships](relationships.md) · [bundles](bundles.md)

Runnable: `RecordFactoryTest`, `RecordProviderIntegrationTest`, `PersistenceGatewayTest`
