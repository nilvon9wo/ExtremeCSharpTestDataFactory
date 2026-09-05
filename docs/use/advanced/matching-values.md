# Keeping a Field Pair in Sync

A common validation shape: two fields — on the same record, or on a parent and
child — must match, or one must be **derived** from the other. XFTY defines the
relationship **once**, in the Provider or on the call.

The `CopyFrom*` classes below are just the bundled, straight-copy
implementations of [`IContextAwareExpression`](../../extend/custom-value-expressions.md).
When the second field is a *transformation* — a boolean from a date, a code
concatenated from a parent's fields, a status mirrored from a child's stage —
write your own small class against the same interface.

---

## Same record — a context-aware sibling

```csharp
.Put(Field.Of<Account>(x => x.ShippingCountry), "Germany")
.Put(Field.Of<Account>(x => x.BillingCity), new CopyFromSiblingExpression(Field.Of<Account>(x => x.ShippingCountry)))
```

Set `ShippingCountry` in one place (Provider default or override template);
`BillingCity` follows. See [context-aware-values](../context-aware-values.md)
— and note the `Put`-ordering rule if `ShippingCountry` is itself context-aware.

### …when it is a transformation, not a copy

<!-- sketch -->
```csharp
.Put(Field.Of<Contact>(x => x.Department), new SiblingCountryLabel())   // "Billing: Germany"
```

<!-- sketch -->
```csharp
public sealed class SiblingCountryLabel : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        string? country = (string?)context.SiblingValue(Field.Of<Contact>(x => x.ReportsToId));
        return $"Billing: {country}";
    }
}
```

Writing and shipping one: [extend/custom-value-expressions](../../extend/custom-value-expressions.md).

---

## Parent and child — a shared ancestor plus a copied field

When many children must all carry a value that lives on their **one** shared
parent:

```csharp
SharedAncestor.Put("hq", new Account { Name = "HQ", OwnerId = someOwnerId })
    .CopyingRelatedField(Field.Of<Account>(x => x.OwnerId));   // children get the Account's OwnerId, not its Id

new MasterTemplate(Field.Of<Case>(x => x.Id))
    .PutRequired(Field.Of<Case>(x => x.AccountId), SharedAncestor.Get("hq"));
```

Every `Case` now carries the shared Account's `OwnerId`. See
[shared-ancestors](../shared-ancestors.md).

---

## Child value up onto a parent

`CopyFromDescendantExpression(childLookupField, sourceField)` copies a value
*up* from a generated child — under `Deferred` (or `.DepthBatched()`), resolved
when the deferred graph is flattened:

```csharp
// on the Account Provider
.Put(Field.Of<Account>(x => x.Site), new CopyFromDescendantExpression(
    Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department)))
```

Any other insert mode throws — the whole graph has to exist first. See
[context-aware-values.md](../context-aware-values.md#reading-up-from-a-child).

Runnable: `ContextAwareExpressionTest`, `CopyFromDescendantExpressionTest`, `SharedAncestorTest`
