# Known Issues

The short list of things XFTY can't do, or does in a way that might catch you
out. Each one: what you tried, what you got, what to do instead.

Fixed bugs and design history are in [CHANGELOG.md](../../CHANGELOG.md) and
[contribute/porting-history.md](../contribute/porting-history.md), not here.

---

## An override template can't force a field to its "empty" value

**You wrote:** an override template — `new Invoice { Total = 0 }`, or
`new Invoice { Notes = null }` — and your Provider has a default for that
field (`Total => 100`, `Notes => "n/a"`).

**You got:** the Provider default. `Total` is `100`, `Notes` is `"n/a"`.

**Why:** XFTY can't tell "I set this to the empty value on purpose" from "I
never touched it". The empty value is `0` / `false` / `default` for a number,
`bool`, `DateTime`, `Guid`, or enum, and `null` for a string, an object, or a
`Nullable<T>` — and in every one of those cases it's what a blank record
already has, so the default fills it. Any *other* value in a template
(`Total = 5`, `Notes = "see attached"`) is kept fine.

**Do this instead**, one of:

- Set it the tracked way on the call: `provider[x => x.Total] = 0`,
  `provider[x => x.Notes] = null` (or `provider.Put(x => x.Total, 0)`). That
  *does* win over a Provider default.
- Drop the Provider's default for this call:
  `provider.RemoveFromMasterTemplate(x => x.Total)` — then the field keeps
  whatever the template gave it (including the empty value).
- If it comes up a lot, a Provider author can simply not default that field.

## A `struct` record only goes so far

`record struct` and `readonly record struct` types generate fine for a plain
unit test (`Mock` or `Never` mode). But:

- **`InsertMode.Now` won't save a `struct`** — the EF Core gateway needs a
  class it can track.
- **A `struct` (or class) with only `{ get; }` properties can't be filled** —
  XFTY needs an `init` or a `set` to write each field.
- **`bundle.Inject(...)` over a graph of structs isn't safe** — every step
  copies the value, so a change made to a nested record later won't show up
  in the parent that holds it. Plain generation (no `Inject`) is fine.

## `bundle.Inject(...)` guesses relationship property names

When `Inject(...)` reshapes a graph so you can read `contact.Account.Name`
straight off the record, it works out that `Contact.AccountId` pairs with a
`Contact.Account` property **by name**: `SomethingId` → `Something`, and a
child list → whichever property is a `List<>` of the child's type.

**If your names don't follow that** — `Contact.AccountFk` with no
`Contact.Account`, or two `List<Case>` properties — `Inject(...)` throws an
error naming the field it couldn't place. There's no attribute or setting to
tell it the right property.

(Your **primary key** is never guessed like this. It's whatever your Provider
declares — `LedgerId`, `Reference`, anything.)

## `ChildProvider` can't check you hung a child on the right field

`provider.With(ChildProvider.For<Case>(x => x.SomeField))` — if `SomeField`
isn't actually a relationship back to the parent, XFTY doesn't stop you at
setup. You find out at generation time, when the child comes back with a
wrong or `null` link. There's no schema to validate against up front.

## A hand-built shared ancestor is checked for an `Id` field

`SharedAncestor.Put("hq", myAccount)` needs to know: is `myAccount` a real
record to use as-is, or a template to generate from? It runs before your
Provider is in scope, so it checks one thing — is a property named `Id` set?

**If your key is named something else** (`AccountId`), a pre-built record
would be mistaken for a template. **Do this instead:** say which you mean —
`SharedAncestor.PutAsValue("hq", myAccount)` for a fixed record, or
`SharedAncestor.PutAsTemplate("hq", myAccount)` to generate from it. (With
`PutAsTemplate`, XFTY notices later that the real key is filled and uses the
record as-is anyway.)

## Shared ancestors and deferred inserts don't reset between test methods

`SharedAncestor` and `DeferredInserter` remember things for the whole test
run — .NET doesn't wipe static state between test methods the way Salesforce
did. A shared ancestor one test registers can bleed into the next.

**Do this instead**, one of:

- put `[IsolatesSharedAncestor]` on the test class (from the `Xfty.Xunit`
  package) — automatic;
- call `SharedAncestor.ResetAllForTesting()` in your test base class or
  fixture;
- or give every test a unique shared-ancestor name
  (`"hq-" + nameof(ThisTestMethod)`).

Full detail in
[salesforce-considerations](salesforce-considerations.md).

## The vector-database packages expect exactly one `float[]`

`Xfty.VectorDatabases.Qdrant` and `.MicrosoftExtensionsVectorData` (both
preview) treat the record's one `float[]` property as the embedding. A record
with two `float[]` fields, or an embedding typed `ReadOnlyMemory<float>`,
throws.

---

## Coming from the Apex original?

A few Apex/Salesforce features have no .NET equivalent. Only relevant if
you're porting from the Apex library —
[salesforce-considerations](salesforce-considerations.md) has the detail.

- **Detecting a Provider variant with zero setup.** In Apex, RecordType
  detection read Salesforce's schema for free. Here you register the
  variants — a `FlavouredLookupKey` / `DiscriminatorLookupKey` with a
  condition on the record — and XFTY *does* then match your override
  template against them automatically (`new Account { Type = "Big" }` finds
  the "Big" variant). `Xfty.EntityFrameworkCore` can even derive the keys
  from a `DbContext`'s discriminator column. What's gone is only the
  "figure it out from metadata with no registration at all" part.
- **Org / environment seeding** — XFTY generates for one test run —
  [use/org-seeding](../use/org-seeding.md).
- **Bundled test-user helpers** — no role/profile schema to resolve against.
- **CPU / row-count budgets** — no fixed per-run quota to track.
- **`RecordInjector` Blob / compound-field / polymorphic machinery** —
  reflection sets any property, so nothing needs special-casing.
