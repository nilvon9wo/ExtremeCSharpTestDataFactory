# Enriching for the Code Under Test — `Inject` / `InjectAll`

A generated graph puts related records on the **bundle** — you read them with
`bundle.GetBundle(...)` chains or `bundle.GetValue(path)`. The **code under
test** can't: it does `contact.Account.Name` or `account.Contacts` straight off
the record, and a plain generated instance carries neither until something
writes them (most of this demo domain's navigation properties are `init`-only).

`Inject` re-expresses the parts of the graph the bundle already holds in that
shape — a populated parent navigation property, a child collection, a forced
scalar — via reflection, so the code under test sees them exactly as it would
after a real query.

It runs **after** generation, returns a **new list of instances**, and never
touches the originals.

> Apex's version did this through a `JSON.serialize` / `JSON.deserialize`
> round-trip, because `SObject.put(...)` rejects relationship and read-only
> fields outright, and needed `XFTY_BlobCarrier` to shepherd a `Blob` field
> through that JSON round-trip intact. Neither is needed here: reflection sets
> any property directly, so there is no round-trip and nothing needs
> special-casing for a `Blob`-shaped field. See
> [sobject-injector](sobject-injector.md).

---

## The target records

Everything below talks about the **target records** — the records `Inject`
operates on. `field` must be one of exactly three things the bundle recognises
(the table below); the bundle it operates on is **whichever bundle you call
`Inject` on** — the one your Provider returned, *or* one you navigated into
(`bundle.GetChildBundle(x)!.Inject(...)`) — never "the root of the original
generation".

| `field` is… | target records | what they can carry |
|---|---|---|
| the **primary** field (`Contact.Id`) | `bundle.PrimaryRecords()` | their generated ancestors; their `With(...)` children |
| a **generated-ancestor** field (`Contact.AccountId`) | `bundle.GetList(Contact.AccountId)` — the Accounts, 1:1 with the primaries | those Accounts' own ancestors; **the inverse child** — the Contacts that generated them |
| a **child-relationship** field (`Case.ContactId`, after `WithChildren`) | `bundle.GetChildList(Case.ContactId)` | each child's own ancestors; its own `With(...)` children |

A `field` the bundle **does not recognise** throws, naming the fields it holds.
A `field` it *does* recognise but generated **nothing** for — a relationship
left out by `SetInclusivity`, a child collection of quantity 0 — has an empty
target list: `Inject` returns an empty list and grafts nothing, it does not
throw.

Relationship fields are resolved to their navigation property by naming
convention (e.g. `Contact.AccountId` → `Contact.Account`, `Account.Id` →
`Account.Contacts`) — the same convention `Field`/`InjectionPathResolver` use
elsewhere in this port; there is no relationship-name string to get right.

---

## `InjectAll` — everything the graph holds

```csharp
Bundle bundle = new RecordProvider(typeof(Contact), lookup)
    .SetInsertMode(InsertMode.Mock)
    .SetInclusivity(InsertInclusivity.Required)
    .SetQuantityPerTemplate(2)
    .SupplyBundle();

List<object> contacts = bundle.InjectAll(Field.Of<Contact>(x => x.Id));
((Contact)contacts[0]).Account!.Name;      // the generated ancestor - was null
((Contact)contacts[0]).Account!.Contacts;  // the inverse child - [ contacts[0] as a plain copy ]
```

`InjectAll(field)` grafts, recursively:

- **every generated ancestor** to `ParentDepth` (5), each carrying its **inverse
  child** — the record one level down that generated it;
- **one level** of every generated child collection, each child carrying **its
  own** generated ancestors.

It **throws** — rather than returning an untouched list — when the graph has no
generated ancestor or child to inject (a Provider run at `None` inclusivity with
no `With(...)`).

`InjectAllParents(field)` and `InjectAllChildren(field)` do one direction only.
None of the three take a config; to configure a broad pass, call `Inject` with
the matching breadth start:

```csharp
bundle.Inject(Field.Of<Contact>(x => x.Id), InjectConfig.AllParents().ParentDepth(2));
```

---

## `Inject(field, config)` — name exactly what you want

Most tests want a focused graph, not everything.

```csharp
// one child collection, nothing else
bundle.Inject(Field.Of<Account>(x => x.Id), InjectConfig.Nothing().InjectChild(Field.Of<Contact>(x => x.AccountId)));
```

```csharp
// a scalar the platform would compute, and a value two hops up
InjectConfig config = InjectConfig.Nothing()
    .InjectValue(Field.Of<Contact>(x => x.Birthdate), new DateTime(2020, 1, 1))
    .InjectValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.AnnualRevenue)], 7_500_000m);

bundle.Inject(Field.Of<Contact>(x => x.Id), config);
```

### The config: a breadth, then refiners

`InjectConfig` starts from a **breadth**:

| Start | Ancestors | Children |
|---|---|---|
| `InjectConfig.Nothing()` | only named | only named |
| `InjectConfig.AllParents()` | every one, to `ParentDepth` | only named |
| `InjectConfig.AllChildren()` | only named | every one, to `ChildDepth`, + the inverse |
| `InjectConfig.Everything()` | every one everywhere | every one everywhere |

then layers refiners on it:

| Refiner | Effect |
|---|---|
| `.InjectParent(path)` | inject the ancestor at `path` **and every hop to it**. `path` is relationship fields from the **target record** — it does not reach into a child's ancestors. |
| `.InjectChild(childLookupField)` | inject the child collection that lookup defines (`Case.ContactId` → the target's Cases). One field, one hop. |
| `.ExcludeParent(path)` / `.ExcludeChild(childLookupField)` | drop anything an exclude covers from a breadth start (prefix match for parents). |
| `.InjectValue(field, value)` | force `value` onto `field` on the **target record**. |
| `.InjectValue(path, value)` | force `value` onto the field at the end of `path`, on a record several hops **up** (materialises the chain to it). Entry-spine only, like `InjectParent`. |
| `.InjectChildValue(childField, leafField, value)` | force `value` onto `leafField` on **every record** of the child collection `childField` defines. |
| `.InjectChildValue(path, value)` | the same, `path` being the child-lookup hops read **downward** then the field to set — a grandchild needs `ChildDepth` (and `BreakSoqlLimits()`) to match. |
| `.ParentDepth(n)` | cap the ancestor climb. Default 5. |
| `.ChildDepth(n)` | how many levels of nested child collections. Default 1; **`n > 1` needs `BreakSoqlLimits()`**. |
| `.BreakSoqlLimits()` | let `ParentDepth`, `ChildDepth` and the `InjectParent` path length exceed what one query could return. |

A forced `value` — record, ancestor or child — may be:

- a **literal** (every record at that position gets it);
- a **`List<object>`** (one per record, in `GetChildList` order; length-checked);
- an **`IValueExpression`**, resolved *fresh per record*.

It **cannot** be a context-aware expression — the pass has no generation
context. A path that never reaches a record the graph produced (a typo, an
ancestor or child that was not generated, a `ParentDepth` / `ChildDepth` too
shallow) is a **loud error**, not a silent no-op.

---

## `Depth` defaults reflect a *future* real query backend

`ParentDepth` defaulting to 5 and `ChildDepth` to 1 mirror Apex's SOQL
relationship-hop and nested-subquery limits — this port has no live database to
actually violate yet, but keeping the same defaults means a graph that would be
awkward to `SELECT` on a real backend is awkward here too, rather than
discovering that the day a persistence layer lands. `BreakSoqlLimits()` lifts
the ceiling when a test genuinely wants a deeper shape.

---

## `SObjectInjector` — the graft mechanism on its own

The graft mechanism is public and needs no bundle. Full guide, with examples:
**[sobject-injector](sobject-injector.md)**.

```csharp
List<object> enriched = SObjectInjector.Inject(contacts)
    .Relationship(Field.Of<Contact>(x => x.Account), accounts)
    .Value(Field.Of<Contact>(x => x.Birthdate), new DateTime(2024, 1, 1))
    .Result();
```

---

## Limits and gotchas

### `Mock` in practice

`Inject` runs off the bundle, so it technically works after any insert mode —
but a value or relationship you forced in is **fiction** were this graph ever
persisted for real. A test that asserts on injected data is valid only as a
**`Mock` unit test** — see [unit-vs-integration](advanced/unit-vs-integration.md).

**Nothing stops a test from treating an injected record as if it were real —
that is deliberate, not enforced — and it is at your own risk.**

### The inverse child on an ancestor is one level of plain copies

`contact` and `contact.Account.Contacts[0]` are distinct instances; the latter
is not re-enriched (`contact.Account.Contacts[0].Cases` is not populated even
if `contact.Cases` is).

### `InjectParent` / `InjectChild` are target-relative

They name relationships from the **target record**. They do not reach into a
child collection's ancestors — that only happens under `Everything()`.

### Runs after generation

An injected value cannot feed a [context-aware value](context-aware-values.md):
that pass ran during generation and is over. To force a value that *depends* on
the graph, read the inputs yourself and pass a literal or a `List<object>`:

```csharp
object? parentName = bundle.GetValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)]);
InjectConfig config = InjectConfig.Nothing()
    .InjectValue(Field.Of<Contact>(x => x.Department), $"{parentName} (contact)");
bundle.Inject(Field.Of<Contact>(x => x.Id), config);
```

`bundle.GetValue(path[, row])`, `bundle.ChildRecordsOf(row, field)` and
`bundle.PrimariesResolvingTo(...)` are the readers for that.

See also: [sobject-injector](sobject-injector.md) · [bundles](bundles.md) ·
[child-records](child-records.md) ·
[value-expressions](value-expressions.md) ·
[unit-vs-integration](advanced/unit-vs-integration.md)

Runnable: `BundleEnricherTest`, `EnrichmentIntegrationTest`, `EnrichmentSelectionTest`
