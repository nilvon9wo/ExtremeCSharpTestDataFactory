# Child Records (`With` / `WithChildren`)

XFTY generates **upward** by default: ask for a `Contact` and it generates the
`Account` the Contact needs. `With(...)` generates the other direction — records
that hang **below** a Provider's primaries.

```csharp
Bundle bundle = new RecordProvider(typeof(Account), lookup)
    .SetInsertMode(InsertMode.Mock)
    .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "Buyer" }).SetQuantity(3))
    .SupplyBundle();

object account       = bundle.PrimaryRecords()![0];
List<object> contacts = bundle.GetChildList<Contact>(x => x.AccountId);
// 1 Account, 3 Contacts, each contact.AccountId == account.Id
```

---

## `ChildProvider`

One child collection. The child record type comes from the **relationship
field**'s declaring type — `Field.Of<Contact>(x => x.AccountId)` is a
property on `Contact`, so the children are Contacts. There is no type argument to
keep in sync.

```csharp
ChildProvider.For<Contact>(x => x.AccountId)                       // blank template
ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "Buyer" })
```

| Method | |
|---|---|
| `.SetQuantity(int)` | children per primary (default 1) |
| `.Put(field, expression \| literal \| contextAwareExpression)` | as on the main Provider |
| `.PutRequired(field, relationship)` / `.PutOptional(field, relationship)` | the child's own relationships |
| `.SetInsertMode(InsertMode)` | default: the parent Provider's. **Cannot mix mock Ids with real DML** either way (`Now`+`Mock` or `Mock`+`Now` throws); ignored under `Deferred`. |
| `.SetInclusivity(InsertInclusivity)` | default: the parent Provider's. Governs the child's **own other** relationships only. |
| `.WithVariant(ILookupKey)` | pin the child Provider variant |
| `.With(ChildProvider)` | nest grandchildren (below) |

> There is no runtime metadata for "what type does this foreign-key-shaped
> property conceptually reference" - a plain reflection `PropertyInfo` only
> exposes its own declaring type - so a relationship field that doesn't
> actually point at the parent type it's hung off isn't caught at
> configuration time. A misconfigured field surfaces as a wrong or `null`
> value instead. See [reference/known-issues.md](../reference/known-issues.md).

## Attaching it

| On `RecordProvider` | |
|---|---|
| `.With(childProvider)` | add a child collection — **repeatable and additive** |
| `.WithChildren(field, n)` | shortcut for `.With(new ChildProvider(field).SetQuantity(n))` |
| `.WithChild(field)` | shortcut for one child |

```csharp
new RecordProvider(typeof(Account), lookup)
    .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "A" }).SetQuantity(3))
    .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "B" }).SetQuantity(2))  // additive
    .With(ChildProvider.For<Case>(x => x.AccountId).SetQuantity(2))                                          // another type
```

---

## Reading the children

| Call | Returns |
|---|---|
| `bundle.GetChild(field)` | the first child for that relationship field |
| `bundle.GetChildList(field)` | every child for that field, merged across configs, in the documented order |
| `bundle.ChildRecordsOf(parentRowIndex, field)` | just the children belonging to `PrimaryRecords()[parentRowIndex]`, read from the recorded parent-of-child map (no arithmetic on `GetChildList`) |
| `bundle.GetChildBundle(field)` | one `Bundle` of all those children — navigate on to the children's **own** generated parents, or to grandchildren |
| `bundle.ChildRelationshipFields()` | every child relationship field populated |

### Order of `GetChildList`

Child rows are produced **config declaration order, then primary order, then
per-primary quantity** — the same "quantity outside the loop" rule as
`SetQuantityPerTemplate` (2 templates × quantity 2 → A, B, A, B).

For two primaries `P0, P1`, config A (quantity 2) then config B (quantity 1):

```text
A/P0  A/P0  A/P1  A/P1   B/P0  B/P1
```

### Working example

```csharp
new RecordProvider(typeof(Account), lookup)
    .SetOverrideTemplateList([new Account(), new Account()])
    .SetQuantityPerTemplate(4)                                                          // 8 Account primaries
    .SetInsertMode(InsertMode.Mock)
    .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "A" }).SetQuantity(3))
    .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "B" }).SetQuantity(2))
    .SupplyBundle();
// 8 primaries × 3 -> 24 department-A Contacts ; 8 x 2 -> 16 department-B ; 40 total
```

---

## Grandchildren

`ChildProvider` nests:

```csharp
new RecordProvider(typeof(Account), lookup)
    .SetInsertMode(InsertMode.Mock)
    .With(
        ChildProvider.For<Contact>(x => x.AccountId).SetQuantity(3)
            .With(ChildProvider.For<Case>(x => x.ContactId).SetQuantity(2)))
    .SupplyBundle();
// per Account: 3 Contacts, and 2 Cases under each Contact (6 Cases)
```

Read them with `bundle.GetChildBundle<Contact>(x => x.AccountId)!.GetChildList<Case>(x => x.ContactId)`.

The row count **multiplies** down the tree.

---

## Insert modes

`SetInsertMode` / `SetInclusivity` on the parent Provider flow through to every
level unless a child overrides them.

| Parent mode | Children |
|---|---|
| `Now` | inserted through the configured `IPersistenceGateway`; throws if none is configured (see [insert-modes](insert-modes.md)). |
| `Mock` | everything gets mock Ids; FKs wired |
| `Never` | nothing persisted; children have a `null` back-reference (no primary Id to point at) — a child can still `SetInsertMode(Mock)` to get its own Ids |
| `Later` | identical to `Never` — the children are generated, nothing is persisted, the back-reference is `null` |
| `Deferred` / `.DepthBatched()` | the **whole** child subtree joins the same deferred graph, generated structurally with FKs wired at flatten time. A per-child `SetInsertMode(...)` override is **ignored** here — the subtree stays structural until the graph is flattened. Flushing that graph to real persistence throws in this port; see [deferred-insert](deferred-insert.md). |

Each child still generates its **own** other required parents (at its
inclusivity) — a `Case` child that needs a `Contact` gets one, and that Contact
gets its Account.

**`.ExcludePrimaryIds()` on the parent does not flow down to children** the
way `SetInsertMode`/`SetInclusivity` do — it only ever excludes the Provider
it's called on. A child collection under an excluded parent still generates
and persists normally, under whatever mode it inherits (or sets itself); it
just has nothing to point its FK at, since the parent it references was
never given an Id, so its own back-reference comes back `null` — see
[insert-modes](insert-modes.md#excluding-the-primary---excludeprimaryids).

### A child cannot mix mock Ids with real DML

A child collection may raise or lower its own insert mode
(`.SetInsertMode(...)` on the `ChildProvider`). The one forbidden combination is
mixing mock Ids with `Now` in either direction: parent `Mock` + child `Now`, or
parent `Now` + child `Mock`, throws `XftyConfigurationException` before either
side even reaches the (always-throwing) persistence layer. Every other pairing
is allowed.

See also: [relationships](relationships.md) · [shared-ancestors](shared-ancestors.md)
(the opposite — many children, **one** shared parent) · [bundles](bundles.md)

Runnable: `ChildProviderTest`
