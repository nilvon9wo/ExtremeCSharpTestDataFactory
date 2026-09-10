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

    private MasterTemplate _template { get; } = new MasterTemplate<Contact>(x => x.Id)
    {
        [x => x.Email] = new UniqueEmailExpression(DefaultEmailPrefix),
        [x => x.FirstName] = new IncrementingStringExpression("Contact First Name"),
        [x => x.LastName] = new IncrementingStringExpression("Contact Last Name"),
    }.PutRequired(x => x.AccountId, new DefaultRelationship(
        new Account { Description = DefaultAccountDescription }));

    public PropertyInfo PrimaryTargetField => Field.Of<Contact>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
```

Almost every Provider is this exact pattern — see this port's own
`AccountDataProvider` / `ContactDataProvider` (`Xfty/Demo/`). `CreateBundle`
returns `Task<Bundle>` and its body is a one-line forward — `GenerationContext`
bundles the Provider Lookup, insert mode, and inclusivity so they travel as one
argument; a Provider rarely inspects it.

`new MasterTemplate<Contact>(x => x.Id)` with a `{ [x => x.Field] = expression }`
initializer is the compact form; the non-generic
`new MasterTemplate(Field.Of<Contact>(x => x.Id)).Put<Contact>(x => x.Field, …)`
is equivalent and reads better for a long fluent chain.

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
[Bundle](../use/bundles.md), the field a relationship points at, and the field
`InsertMode.Mock` / `Now` fills with an identifier:

<!-- sketch -->
```csharp
public PropertyInfo PrimaryTargetField => Field.Of<Contact>(x => x.Id);
```

**Nothing is hard-coded to a property named `Id`.** Declare whatever your
record actually uses — `LedgerId`, `Reference`, `OrderRef` — and XFTY threads
*that* field through relationship wiring and the whole persistence path
(including the depth-batched and deferred inserters):

<!-- sketch -->
```csharp
private MasterTemplate _template { get; } = new MasterTemplate<Ledger>(x => x.LedgerId);

public PropertyInfo PrimaryTargetField => Field.Of<Ledger>(x => x.LedgerId);
```

The key's **type** is equally open. Under `InsertMode.Mock`,
`DefaultMockIdGenerator` mints a `string`, `int`, `long`, or `Guid` to match
the field; any other type needs an
[`IMockIdGenerator`](mock-id-generators.md). A key already set on the override
template is kept as-is.

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
