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
.Put<Account>(x => x.ShippingCity, "Berlin")
.Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCity))
```

`BillingCity` is filled from whatever `ShippingCity` ends up being.

---

## Copy a field from a generated ancestor

One hop — a relationship field then the field to read:

```csharp
.PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account()))
.Put<Contact>(x => x.Department, CopyFromAncestorExpression.From<Contact, Account>(x => x.AccountId, x => x.Site))
```

Several hops — a path of relationship fields ending in the field to read:

```csharp
.Put<Case>(x => x.Subject, new CopyFromAncestorExpression([
    Field.Of<Case>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId), Field.Of<User>(x => x.LastName),
]))
```

`CopyFromAncestorExpression` returns `null` if any hop of the relationship was
not generated (e.g. an optional one skipped by the current inclusivity).

---

## Your own logic

Implement `IContextAwareExpression` — one method:

```csharp
public class IsMinorFlag : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        DateTime? birthdate = (DateTime?)context.SiblingValue(Field.Of<Contact>(x => x.Birthdate));
        return birthdate is not null && birthdate.Value.AddYears(18) > DateTime.Today ? "MINOR" : "ADULT";
    }
}
```

```csharp
.Put<Contact>(x => x.Birthdate, new DateTime(2010, 1, 1))
.Put<Contact>(x => x.Department, new IsMinorFlag())
```

`context` (a `GenerationContext`) exposes:

- **`SiblingValue(field)`** — the final value of another field on this record.
  Prefer this over `context.RecordBeingBuilt.GetType().GetProperty(...).GetValue(...)`:
  it returns the same value but throws a clear error if `field` is another
  context-aware value that has not been generated yet, instead of handing back
  a misleading `null`.
- **`BundleSoFar`** — everything this generation call has built: the generated
  parents (`GetList(relationshipField)`) **and** the sibling primary records
  (`GetList(<primaryField>)`, e.g. `GetList(Field.Of<Account>(x => x.Id))`).
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
.Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCity))
.Put<Account>(x => x.ShippingCity, CopyFromSiblingExpression.From<Account>(x => x.Site))   // throws at generation
```

An override-template value still wins over a context-aware expression.

---

## Reading up from a child

`CopyFromDescendantExpression` copies a field from a generated **child** — the
record that references this one through the given lookup field:

```csharp
// on an Account Provider, so a validation rule comparing the two passes
.Put<Account>(x => x.Site, CopyFromDescendantExpression.From<Contact>(x => x.AccountId, x => x.Department))
```

The child does not exist when the parent is built, so this needs the whole graph
in memory first: **it only works under `Deferred` (or `.DepthBatched()`)** and is
resolved when the deferred graph is flattened. A Provider that carries one of
these in any other insert mode **throws** — it does not silently leave the field
`null`.

> `DeferredInserter.Flush(gateway)` and a `.DepthBatched()` `Now` call both
> insert for real through a configured `IPersistenceGateway`; with none
> configured, both throw `NotSupportedException` instead. Also always
> available, with or without a gateway: building the whole deferred graph in
> memory and reading the resolved up-flow value straight
> off `DeferredInsertBuffer.Flatten(bundle)`, which runs the same resolution
> pass without needing to insert anything. See
> [deferred-insert](deferred-insert.md) and
> [reference/known-issues.md](../reference/known-issues.md).

Works whether the child is a generated ancestor's requesting child or one of a
parent's `WithChildren` rows. With more than one matching child the **first** is
read at every hop; with none, the value is `null`.

Several hops — a path of child-lookup fields ending in the field to read,
mirroring `CopyFromAncestorExpression`'s own path form:

```csharp
.Put<Account>(x => x.Description, new CopyFromDescendantExpression([
    Field.Of<Contact>(x => x.AccountId), Field.Of<Case>(x => x.ContactId), Field.Of<Case>(x => x.Subject),
]))
```

Reads the `Subject` of the first generated Case belonging to the first
generated Contact under this Account - two hops down. `null` if either hop
has no match. Reading an aggregate across many children at one hop (not just
the first) is not built as a bundled expression, but a custom
`IDeferredExpression` can already do it: `DeferredGraph.ChildIndicesOf`/
`ChildrenOf` both return every match, not just the first.

---

Writing custom expressions as a distributable extension:
[extend/custom-value-expressions.md](../extend/custom-value-expressions.md).

Runnable: `ContextAwareExpressionTest`, `CopyFromSiblingExpressionTest`, `CopyFromAncestorExpressionTest`, `CopyFromDescendantExpressionTest`
