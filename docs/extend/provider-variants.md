# Registering Provider Variants

A single record type can have several Providers, chosen by a **lookup key**.
This page is about **registering** variants; selecting one as a consumer is
[use/provider-variants](../use/provider-variants.md).

> Apex's variant system also had `XFTY_RecordTypeLookupKey` — a Salesforce
> `RecordTypeId` discriminator, resolved via schema describe with no SOQL. That
> is genuinely Salesforce-specific and has no C# analog; it is not ported. See
> [reference/known-issues.md](../reference/known-issues.md).

---

## The key types

| Key | Selects by | Specificity |
|-----|-----------|-------------|
| `LookupKey.Get(type)` | record type only (the default) | 0 |
| `FlavouredLookupKey.Get(type, flavour).Matching(predicate)…` | record type + arbitrary conditions on the record | 20 + predicate count |
| your own `ILookupKey` | anything | you choose |

All keys are flyweights — obtain with `.Get(...)`, never `new`. A
`FlavouredLookupKey` is interned by type + flavour (its predicates are *not*
part of its identity); add its predicates with `.Matching(...)` **once**.

---

## Predicates on a flavoured key

`.Matching(...)` takes any `IRecordPredicate` — a one-method interface
(`bool IsSatisfiedBy(object? record)`). Repeated `.Matching(...)` calls are an
**AND**.

**Ready-made single-field conditions** — `FieldPredicateFactory`:

| Factory | Matches when |
|---------|-------------|
| `EqualTo(field, value)` / `NotEqualTo(field, value)` | `field` == / != `value` (null-aware) |
| `GreaterThan(field, value)` / `LessThan(field, value)` | numeric or `DateTime`, else lexicographic; false if either side is null |
| `IsNull(field)` / `IsNotNull(field)` | `field` is / is not null |
| `InSet(field, values)` | `field` is one of the set (null set → matches nothing) |

(`FieldPredicateFactory` is a thin facade — each factory wires up a
purpose-built class such as `FieldGreaterThanPredicate` or
`FieldInSetPredicate`, and `NotEqualTo` / `IsNotNull` are a negated `EqualTo`.
Use those classes directly if you prefer.)

**AND / OR / NOT** — `PredicateFactory`, for anything beyond the implicit AND:

<!-- sketch -->
```csharp
FlavouredLookupKey.Get(typeof(Account), "strategic")
    .Matching(PredicateFactory.AnyOf([
        FieldPredicateFactory.GreaterThan(Field.Of<Account>(x => x.AnnualRevenue), 1_000_000m),
        FieldPredicateFactory.GreaterThan(Field.Of<Account>(x => x.NumberOfEmployees), 5000),
    ]))
    .Matching(PredicateFactory.Negate(FieldPredicateFactory.EqualTo(Field.Of<Account>(x => x.Type), "Prospect")));
```

`AllOf(list)` / `AnyOf(list)` / `Negate(one)` return an `IRecordPredicate`, so
they nest. An empty `AllOf` is vacuously true; an empty `AnyOf` is never
satisfied.

**Your own predicate** — when the ready-made ones do not express the
condition, implement the interface. No base class, no registration:

<!-- sketch -->
```csharp
public sealed class CreatedThisYearPredicate : IRecordPredicate
{
    public bool IsSatisfiedBy(object? record) =>
        record?.GetType().GetProperty("CreatedDate")?.GetValue(record) is DateTime created
        && created >= new DateTime(DateTime.Today.Year, 1, 1);
}
```

---

## Define keys in one place

A flavoured key is referenced from the Provider Lookup map *and* from every
relationship that pins that variant, so define each in a shared `*LookupKeys`
constants class:

<!-- sketch -->
```csharp
public static class MyProjectLookupKeys
{
    public static readonly ILookupKey EnterpriseAccount =
        FlavouredLookupKey.Get(typeof(Account), "enterprise")
            .Matching(FieldPredicateFactory.GreaterThan(Field.Of<Account>(x => x.NumberOfEmployees), 1000));
}
```

<!-- sketch -->
```csharp
private static readonly Dictionary<ILookupKey, Type> Providers = new()
{
    [LookupKey.Get(typeof(Account))]          = typeof(BusinessAccountProvider),
    [MyProjectLookupKeys.EnterpriseAccount]   = typeof(EnterpriseAccountProvider),
};
```

---

## Resolution

- **Explicit:** `lookup.Get(someKey)`.
- **Top-level generation** picks a variant via `WithVariant(key)`, or the
  lookup-key constructor — see [use/provider-variants](../use/provider-variants.md).
- **A relationship with an explicit key:**
  `new DefaultRelationship(MyProjectLookupKeys.EnterpriseAccount, new Account())`.
- **A relationship with only an override template:**
  `ProviderLookups.Resolve` collects every registered key whose
  `IsInstanceOf(template)` is true and picks the most specific; the plain type
  key is the fallback. Two equally-specific matches is an error — supply an
  explicit key. The derived key is memoised on the relationship.
- **An explicit key *and* an override template that disagree:** if the template
  independently matches a *different* refined variant, that is a contradiction
  and throws rather than silently letting the explicit key win. A template that
  matches no flavour's predicates is fine — the explicit key stands.

Each top-level Provider still owns one Master Template, so one generation call
produces one variant.

---

## Your own lookup key

`ILookupKey` is four members — implement it directly when a variant is chosen
by something the shipped keys don't model. `IsInstanceOf(object?)` is what
template-derived resolution calls; `Specificity` decides who wins when several
keys match (return more than `20` to outrank a flavoured key). Register the
instance in the Provider map like any other key.

<!-- sketch -->
```csharp
public sealed class WholesaleAccountKey : ILookupKey
{
    public Type RecordType => typeof(Account);

    public bool IsInstanceOf(object? record) =>
        record is Account { Industry: "Wholesale", AnnualRevenue: not null };

    public string HashKey => "Account::wholesale";

    public int Specificity => 30;
}
```

Runnable: `MultiVariantProviderTest`, `VariantResolutionTest`, `PredicateFactoryTest`, `FieldPredicateFactoryTest`
