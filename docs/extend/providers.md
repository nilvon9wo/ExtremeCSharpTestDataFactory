# Writing a Provider

A Provider teaches XFTY how to generate test data for one record type — its
default values, its relationships, and how they should be generated. Providers
are **declarative**: they describe what valid test data looks like, they do not
imperatively build records.

Related: [provider-lookups](provider-lookups.md) (registering Providers) ·
[provider-variants](provider-variants.md) (more than one Provider per type) ·
[bundled-providers](bundled-providers.md) (the shipped Providers) ·
[custom-value-expressions](custom-value-expressions.md).

---

## The shape

A Provider implements `IRecordProvider`:

<!-- sketch -->
```csharp
public sealed class MyContactProvider : IRecordProvider
{
    public const string DefaultEmailPrefix = "test.contact";
    public const string DefaultAccountDescription = "Account for contact";

    private static readonly PropertyInfo PrimaryField = Field.Of<Contact>(x => x.Id);

    private static readonly MasterTemplate Template = new MasterTemplate(PrimaryField)
        .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(
            new Account { Description = DefaultAccountDescription }))
        .Put<Contact>(x => x.Email, new UniqueEmailExpression(DefaultEmailPrefix))
        .Put<Contact>(x => x.FirstName, new IncrementingStringExpression("Contact First Name"))
        .Put<Contact>(x => x.LastName, new IncrementingStringExpression("Contact Last Name"));

    public PropertyInfo PrimaryTargetField => PrimaryField;

    public MasterTemplate MasterTemplate => Template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, Template, templateRecords);
}
```

Almost every Provider is this exact pattern. `CreateBundle`'s body is a one-line
forward — `GenerationContext` bundles the Provider Lookup, insert mode, and
inclusivity so they travel as one argument; a Provider rarely inspects it.

---

## The Master Template

The declarative heart. It holds maps keyed by `PropertyInfo`: default values,
context-aware values, deferred (up-flow) values, required relationships,
optional relationships. Fluent builders:

| Method | Adds |
|--------|------|
| `Put(field, expression)` / `Put(field, literal)` | a [value expression](../use/value-expressions.md) (a bare value is wrapped as `LiteralExpression`) |
| `Put(field, contextAwareExpression)` | a [context-aware value](../use/context-aware-values.md) |
| `Put(field, deferredExpression)` | an [up-flow value](custom-value-expressions.md) — needs `Deferred` |
| `PutRequired(field, relationship)` | a required [relationship](../use/relationships.md) |
| `PutOptional(field, relationship)` | an optional relationship |

The untyped `Put(field, object? value)` overload routes by the runtime type of
`value` and **throws** on an `IDefaultRelationship` — it cannot tell required
from optional, so relationships always need `PutRequired` / `PutOptional`
explicitly.

Keep it declarative — describe data, not algorithms. No conditional logic that
builds records by hand.

---

## Primary Target Field

Every Provider declares the field that identifies its primary records inside a
[Bundle](../use/bundles.md). For nearly every record type this is `Id`:

<!-- sketch -->
```csharp
private static readonly PropertyInfo PrimaryField = Field.Of<Contact>(x => x.Id);
```

A configurable field (rather than a hard-coded `Id`) keeps the engine
independent of the few object types that identify records differently.

---

## Relationship design

For every relationship, ask: *can this object reasonably exist without the
related record?*

- **No** → `PutRequired(field, new DefaultRelationship(...))`
- **Yes** → `PutOptional(field, new DefaultRelationship(...))`

Prefer optional. Every required relationship enlarges every generated graph and
slows every test. Model only genuinely-required relationships as required.

The record passed to `DefaultRelationship` is an override template for the
generated parent; its remaining fields come from that parent's own Provider.

---

## Testing a Provider

Every new Provider gets its own test class verifying: records generate;
required relationships populate; optional relationships behave correctly;
unique values stay unique. A failing Provider test is far easier to diagnose
than dozens of unrelated application tests failing because a data shape
changed. `Xfty.Test/Demo/AccountDataProviderTest.cs` and
`ContactDataProviderTest.cs` are worked examples for this port's own bundled
Providers.

Runnable: `AccountDataProviderTest`, `ContactDataProviderTest`
