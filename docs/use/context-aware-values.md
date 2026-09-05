# Context-Aware Values

Most [value expressions](value-expressions.md) generate a field in isolation. A
**context-aware** value sees the rest of the record — a field copied from a
sibling, from a generated parent, or (under `Deferred`) from a generated child.

`IContextAwareExpression` is a separate interface from `IValueExpression` (a
context-aware value has no meaningful no-argument `Get()`), but `Put(...)`
accepts it directly.

---

## Copy a sibling field

```csharp
.Put(Field.Of<Account>(nameof(Account.ShippingCity)), "Berlin")
.Put(Field.Of<Account>(nameof(Account.BillingCity)), new CopyFromSiblingExpression(Field.Of<Account>(nameof(Account.ShippingCity))))
```

`BillingCity` is filled from whatever `ShippingCity` ends up being.

---

## Copy a field from a generated ancestor

One hop — a relationship field then the field to read:

```csharp
.PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), new DefaultRelationship(new Account()))
.Put(Field.Of<Contact>(nameof(Contact.Department)), new CopyFromAncestorExpression(
    Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Account>(nameof(Account.Site))))
```

Several hops — a path of relationship fields ending in the field to read:

```csharp
.Put(Field.Of<Case>(nameof(Case.Subject)), new CopyFromAncestorExpression([
    Field.Of<Case>(nameof(Case.AccountId)), Field.Of<Account>(nameof(Account.ParentId)), Field.Of<Account>(nameof(Account.Name)),
]))
```

`CopyFromAncestorExpression` returns `null` if any hop of the relationship was
not generated (e.g. an optional one skipped by the current inclusivity).

---

## Your own logic

Implement `IContextAwareExpression` — one method:

```csharp
public class IsAdultFlag : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        DateTime? birthdate = (DateTime?)context.SiblingValue(Field.Of<Contact>(nameof(Contact.Birthdate)));
        return birthdate is not null && birthdate.Value.AddYears(18) <= DateTime.Today;
    }
}
```

```csharp
.Put(Field.Of<Contact>(nameof(Contact.Birthdate)), new DateTime(2000, 1, 1))
.Put(Field.Of<Contact>(nameof(Contact.Department)), new IsAdultFlag())
```

`context` (a `GenerationContext`) exposes:

- **`SiblingValue(field)`** — the final value of another field on this record.
  Prefer this over `context.RecordBeingBuilt.GetType().GetProperty(...).GetValue(...)`:
  it returns the same value but throws a clear error if `field` is another
  context-aware value that has not been generated yet, instead of handing back
  a misleading `null`.
- **`BundleSoFar`** — everything this generation call has built: the generated
  parents (`GetList(relationshipField)`) **and** the sibling primary records
  (`GetList(<primaryField>)`, e.g. `GetList(Field.Of<Account>(nameof(Account.Id)))`).
- **`RowIndex`** — which row of a multi-record generation this is.

---

## How it runs, and the one ordering rule

Values are filled in two passes: plain expressions first, then context-aware
expressions **in the order they were `Put(...)`**. So a context-aware value can
read any plain field, any wired lookup, and any *earlier* context-aware value.

Reading a *later* context-aware value — or a circular pair — throws a clear error
naming both fields and the `Put` order that fixes it. It is never a silent
`null`. (A sibling that genuinely generated to `null` is returned as `null`; only
a not-yet-generated one throws.)

```csharp
// wrong - BillingCity reads ShippingCity, but ShippingCity is put after it
.Put(Field.Of<Account>(nameof(Account.BillingCity)), new CopyFromSiblingExpression(Field.Of<Account>(nameof(Account.ShippingCity))))
.Put(Field.Of<Account>(nameof(Account.ShippingCity)), new CopyFromSiblingExpression(Field.Of<Account>(nameof(Account.Site))))   // throws at generation
```

An override-template value still wins over a context-aware expression.

---

## Reading up from a child

`CopyFromDescendantExpression` copies a field from a generated **child** — the
record that references this one through the given lookup field:

```csharp
// on an Account Provider, so a validation rule comparing the two passes
.Put(Field.Of<Account>(nameof(Account.Site)), new CopyFromDescendantExpression(
    Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Contact>(nameof(Contact.Department))))
```

The child does not exist when the parent is built, so this needs the whole graph
in memory first: **it only works under `Deferred` (or `.DepthBatched()`)** and is
resolved when the deferred graph is flattened. A Provider that carries one of
these in any other insert mode **throws** — it does not silently leave the field
`null`.

> **This port has no persistence layer.** `DeferredInserter.Flush()` and a
> `.DepthBatched()` `Now` call both always throw `NotSupportedException` here —
> there is nothing to insert into. What *is* proven and usable: building the
> whole deferred graph in memory and reading the resolved up-flow value straight
> off `DeferredInsertBuffer.Flatten(bundle)`, which runs the same resolution
> pass without needing to insert anything. See
> [deferred-insert](deferred-insert.md) and
> [reference/known-issues.md](../reference/known-issues.md).

Works whether the child is a generated ancestor's requesting child or one of a
parent's `WithChildren` rows. With more than one matching child the **first** is
read; with none, the value is `null`. Multi-hop paths and aggregates across
children are not built.

---

Writing custom expressions as a distributable extension:
[extend/custom-value-expressions.md](../extend/custom-value-expressions.md).
