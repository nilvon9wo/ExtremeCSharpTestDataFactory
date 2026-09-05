# Shared Ancestors

By default every generated child gets its **own** generated parent. When several
children should sit under **one** parent — 50 Contacts at the same Account, a
whole hierarchy converging on one root — use `SharedAncestor`.

---

## One API — resolution is automatic

There is nothing to declare or opt into. A shared ancestor is registered once —
by the [lookup that ships the Providers](#packaged-defaults) for the common case,
or by the test — and referenced anywhere; XFTY works out how to resolve it.

Before a Provider generates anything, every shared ancestor configured so far in
the process is resolved in one place, each honouring the call's insert mode.
XFTY inspects each one's Provider:

| The shared ancestor's Provider… | How it resolves |
|---|---|
| **has no relationships of its own** ("flat" — a plain `Account`, a bare lookup value) | one record, resolved once |
| **pulls in ancestors of its own** ("deep" — a heavy graph) | its whole sub-graph is built once, one dependency layer at a time — the fewest resolution passes; a chain converging on a singleton root collapses to one shared sub-graph |

Either way: **one record, one Id, everywhere** — and it is generated at most once
per process.

> **Apex resets `static` state between test methods; this port does not.**
> `SharedAncestor`'s registry is a `static` `Dictionary` that lives for the whole
> test run, not per test method — there is no xUnit equivalent of Apex's
> automatic per-method reset. Give every test's shared ancestors a **name unique
> to that test**, and see
> [reference/known-issues.md](../reference/known-issues.md) for the cleanup this
> implies for a test that deliberately leaves one unresolved.

---

## The simplest case

<!-- sketch -->
```csharp
// register once - centrally for shipped Providers (see "Packaged defaults"), or in the test
SharedAncestor.Put("acme-hq", new Account { Name = "ACME HQ" });

// reference it from any Master Template, any field, required or optional
new MasterTemplate(Field.Of<Contact>(x => x.Id))
    .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get("acme-hq"));
```

<!-- sketch -->
```csharp
List<object> contacts = new RecordProvider(typeof(Contact), lookup)
    .SetQuantityPerTemplate(50)
    .SetInclusivity(InsertInclusivity.Required)
    .SetInsertMode(InsertMode.Mock)
    .SupplyList();
// -> 50 Contacts, ONE generated Account, ONE mock Id assigned
```

- **One record, one Id.** Every child that references `"acme-hq"` gets the same
  `Account` instance and the same `AccountId`.
- **Generated once.** Every reference — in the same or a later `Supply*()`
  call — reuses it.
- **Persistence follows the call.** `Mock` gives it a mock Id, `Never` leaves it
  Id-less. (`Now` would insert it, but this port's `Now` always throws — see
  [insert-modes](insert-modes.md).) A `.DepthBatched()` / `Deferred` call
  resolves its shared ancestors **up front** (so their Ids are ready when the
  deferred graph is flattened) rather than deferring them.

---

## A deep shared ancestor

Nothing extra to do — configure the rungs and reference the leaf:

<!-- sketch -->
```csharp
// once, centrally
SharedAncestor.Put("root", new Account { Name = "Global HQ" });
SharedAncestor.Put("region", new Account { Name = "Region HQ" })
    .PutRequired<Account>(x => x.ParentId, SharedAncestor.Get("root"));
// a Contact Provider does PutRequired(Contact.AccountId, SharedAncestor.Get("region"))
```

- **A shared ancestor referenced by another shared ancestor's Provider is pulled
  in automatically**, resolved before the one that needs it — you do not list
  every rung.
- **Depth-batched, mode-aware.** Each deep shared ancestor's sub-graph is
  resolved one dependency layer at a time, mock-Id'd (`Mock`) or left in memory
  (`Never`).
- **A cycle (`a` needs `b`, `b` needs `a`) throws** — break it by pre-registering
  one side with `SharedAncestor.Put(name, record)`.

---

## Registering

`Put(name, ...)` registers; `Get(name)` only retrieves (the token to hand to
`PutRequired` / `PutOptional`, and the handle for `ResolveNow` / `GetId`).

| Call | Effect |
|------|--------|
| `SharedAncestor.Get(name)` | the interned shared ancestor for `name` — retrieval only |
| `SharedAncestor.Put(name, object? record)` | register `record`. **Id present** → a fixed value; **no Id** → an override template. Use the explicit forms to be sure |
| `SharedAncestor.PutAsTemplate(name, object? template)` | always an override template (generated in the pre-phase; also sets the type) |
| `SharedAncestor.PutAsValue(name, object record)` | always used as-is |
| `SharedAncestor.Put(name, ILookupKey key)` | register just the Provider variant that generates it ([provider-variants](provider-variants.md)) |
| `.FromVariant(ILookupKey key)` | chained off `Put(name, …)` — pin the variant *and* keep the template |
| `.Put(field, …)` · `.PutRequired(field, rel)` · `.PutOptional(…)` · `.IncludeOptional(…)` · `.Put(path, …)` · `.SetInclusivity(…)` | chained onto `Put(name, …)` — shape the shared record's own generation, exactly the API a generated parent takes (see below) |
| `.CopyingRelatedField(field)` | copy `field` from the shared record into the child's field instead of its Id |
| `SharedAncestor.PutIfAbsent(name, template)` | `PutAsTemplate`, only if `name` is not registered yet — for a shared setup helper that may run more than once, or that registers more ancestors than one test uses |
| `SharedAncestor.PutIfAbsent(name, lookupKey)` | as above, pinning the Provider variant instead of a template |

Re-registering a shared ancestor after it has resolved throws.

### Shaping the shared record's own generation

When a bare template / key is not enough — value expressions on the shared
record, or its *own* ancestors — chain the same `Put` API a generated parent
takes straight onto `Put(name, …)`:

<!-- sketch -->
```csharp
SharedAncestor.Put("hq", new Account { Name = "HQ Ltd" })
    .Put<Account>(x => x.Site, "Berlin")
    .PutRequired<Account>(x => x.ParentId, new DefaultRelationship(new Account { Name = "Global HQ" }))
    .SetInclusivity(InsertInclusivity.Required)
    .IncludeOptional<Account>(x => x.OwnerId)
    .Put([Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.Site)], "Global");
```

Only the methods that make sense for **one record** are on it — there is no
`SetQuantityPerTemplate`, no template list, no `SetInsertMode` (persistence
follows the referencing call, or `ResolveNow(lookup, mode)`), no `.DepthBatched()`
(the resolver already depth-batches the sub-graph), no child collections. Nothing
to reject at runtime; those knobs simply are not there.

**The shared record's own field values go on this chain** — it is one record for
every child, so there is no per-call place to set them. A
`Put([theSharedRelationshipField, deeperField], value)` that would *set a value
on* a shared ancestor ([per-call ancestor values](per-call-relationships.md))
**throws**. Wiring a shared ancestor **in** as a relationship value —
`PutRequired([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)], SharedAncestor.Get("mr-smith"))` —
is fine.

---

## Packaged defaults

A Provider you ship should work without every consuming test knowing its shared
ancestors' names. Put the defaults on the **lookup** — the package boundary a
consumer already depends on.

The quick form: pass them alongside the Provider map.

<!-- sketch -->
```csharp
ProviderLookups.Of(
    new Dictionary<ILookupKey, IRecordProvider>
    {
        [LookupKey.Get(typeof(Account))] = new MyAccountProvider(),
        [LookupKey.Get(typeof(Contact))] = new MyContactProvider(),   // references Get("acme-hq")
    },
    new Dictionary<string, object> { ["acme-hq"] = new Account { Name = "ACME HQ" } });
```

A hand-written lookup implements the companion interface
**`ISharedAncestorDefaults`** — one method:

<!-- sketch -->
```csharp
public sealed class MyProjectLookup : IProviderLookup, ISharedAncestorDefaults
{
    // ... the usual Get / KeysFor ...
    public void RegisterSharedAncestorDefaults()
    {
        SharedAncestor.PutIfAbsent("acme-hq", new Account { Name = "ACME HQ" });
    }
}
```

XFTY calls it before each `Supply*()` resolves shared ancestors. Because it uses
**`PutIfAbsent`**, a test that wants a different shared record just registers it
first — the default is skipped. A lookup with no shared ancestors does not
implement the interface.

---

## Supplying your own record, and reading the Id

Continuing the examples above (illustrative — reusing `"root"` / `"acme-hq"`
here as a reminder of the earlier sections, not as a second, independent
registration of the same name in the same run):

<!-- sketch -->
```csharp
Account root = /* the test builds its own singleton root, e.g. with InsertMode.Mock */;
SharedAncestor.Put("root", root);   // from here, Get("root") resolves to this

object hqId = SharedAncestor.GetId("acme-hq");  // after it has resolved
```

`GetId(name)` throws if the ancestor has not been resolved yet. To read it
**before** any `Supply*()` call, resolve it explicitly:

<!-- sketch -->
```csharp
SharedAncestor.Get("root").ResolveNow(lookup, InsertMode.Mock);
object rootId = SharedAncestor.GetId("root");
```

`ResolveNow(lookup, mode)` also fixes the shared record's own insert mode
independently of the call that first references it.

---

## Gotchas

- **One insert mode per shared ancestor.** If it is first resolved `Mock` and
  then referenced from a call using a different mode, XFTY throws a clear
  "consistent insert mode" error rather than drift a mock Id into real
  persistence.
- **A cycle throws.** Two shared ancestors that need each other, or one whose
  Provider references it back — break it with `Put(name, record)`.
- **Independent heavy sub-graphs each get their own resolution pass.**
  Resolution depth-batches *per* shared-ancestor sub-graph, not one pass across
  all of them. Converging chains (a shared root) are already one pass.

---

## Controlling what gets resolved

By default every registered shared ancestor is resolved in the pre-phase — the
ones your test registered plus any [packaged defaults](#packaged-defaults). Two
knobs hand control back to the test:

| Call | Effect |
|------|--------|
| `SharedAncestor.Disable(name)` | never resolved; every reference leaves the child's FK **null**. For null-scenario tests, or dropping a heavy default this test does not need. `GetId(name)` on it throws. |
| `SharedAncestor.ManualResolutionOnly()` | turns off the pre-phase. A **lightweight** shared ancestor (no sub-graph of its own — auto-detected) still resolves on-demand when first referenced. A **heavy** one throws unless it was resolved up front. |
| `SharedAncestor.Get(name).ResolveNow(lookup, mode)` | resolve one (and its chain) up front |
| `SharedAncestor.ResolveNow(lookup, mode, names)` | resolve a named set up front, one depth-batched pass |

Not exercised by this port's own test suite — `ManualResolutionOnly()` has no
unsetter and is not safely testable in a shared xUnit process (see the warning
below).

<!-- sketch -->
```csharp
SharedAncestor.ManualResolutionOnly();
SharedAncestor.ResolveNow(lookup, InsertMode.Mock, ["division", "region"]);
// the package's other shared-ancestor defaults are never built
```

> **`ManualResolutionOnly()` has no unsetter, in Apex or here — and here it is a
> single `static` flag for the whole process, not per test method.** A test that
> calls it changes every later test in the same run that relies on
> auto-resolution. Treat it as effectively global and avoid it in a shared xUnit
> process unless you are certain nothing else in the run depends on the
> pre-phase; see [reference/known-issues.md](../reference/known-issues.md).

---

## In a shipped Master Template

Putting a `SharedAncestor` in a Provider you distribute (rather than on a
`RecordProvider` instance in one test) is an *extend* concern — see
[extend/shared-ancestors-in-templates.md](../extend/shared-ancestors-in-templates.md).

See also: [relationships](relationships.md) · [bundles](bundles.md) ·
[insert-modes](insert-modes.md)

Runnable: `SharedAncestorTest`, `SharedAncestorHierarchyTest`
