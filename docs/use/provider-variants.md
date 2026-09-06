# Choosing a Provider Variant

A single record type can have several Providers — a "Business Account" vs a
VIP Account, or any other "flavour" a project defines. This page is about
**selecting** one as a consumer. **Registering** variants is an *extend* task —
[extend/provider-variants.md](../extend/provider-variants.md).

A variant is identified by a **lookup key** the project exposes, usually as a
constant:

<!-- sketch -->
```csharp
MyProjectLookupKeys.VipAccount   // an ILookupKey
```

> A discriminator-field variant - matching a Provider by a field's value, e.g.
> "`Type == Person`" on a shared record type - is `DiscriminatorLookupKey`,
> built over the same `FlavouredLookupKey` mechanism as everything else here.
> The general variant system is `FlavouredLookupKey`: a record type plus one
> or more [predicates](../extend/provider-variants.md) evaluated against the
> override template.

---

## Three ways to pick one

### `WithVariant(key)`

<!-- sketch -->
```csharp
await new RecordProvider(typeof(Account), lookup)
    .WithVariant(MyProjectLookupKeys.VipAccount)
    .Supply();
```

Must be called **before** any `Put(...)` — the Master Template is derived from
the resolved Provider (it throws otherwise).

### The lookup-key constructor

<!-- sketch -->
```csharp
await new RecordProvider(MyProjectLookupKeys.VipAccount, lookup)
    .Supply();
```

Same effect as `WithVariant`, and takes the record type from the key.

### An override template that matches a flavour's predicates

<!-- sketch -->
```csharp
await new RecordProvider(new Account { AnnualRevenue = 5_000_000m }, lookup)
    .Supply();
```

XFTY matches the template against every registered `FlavouredLookupKey`'s
predicates and selects the matching Provider automatically, provided the
flavour was registered with at least one predicate (a flavour with none can
only be selected explicitly).

Don't combine the two: `WithVariant(key)` *and* an override template that
matches a *different* registered flavour throws. Pick one.

---

## For a related record

When a relationship should generate a specific variant of its parent, pin it on
the relationship:

<!-- sketch -->
```csharp
.PutRequired<Case>(x => x.AccountId, new DefaultRelationship(
    MyProjectLookupKeys.VipAccount, new Account()))
```

Without an explicit key, the parent's variant is derived from the override
template the relationship carries.

See also: [extend/provider-variants](../extend/provider-variants.md) · [relationships](relationships.md)

Runnable: `MultiVariantProviderTest`, `VariantResolutionTest`, `LookupKeyTest`
