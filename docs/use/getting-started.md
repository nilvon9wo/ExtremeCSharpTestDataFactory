# Getting Started

This guide introduces the core concepts of XFTY and demonstrates the most common ways of generating test data.

After reading this guide you should be comfortable:

- generating records
- customizing individual fields
- creating related records
- understanding Bundles
- choosing insert modes
- deciding when relationships should be created

More advanced topics such as implementing Providers and writing custom value expressions are covered in later guides.

> **This port has no persistence layer yet.** `InsertMode.Now` — Apex's
> integration-test mode — always throws `NotSupportedException` here (see
> [insert-modes](insert-modes.md)). Everything below uses `Mock`, which is this
> port's practical default: realistic-looking Ids, nothing persisted.

---

# Creating Your First Record

The simplest way to use XFTY is to request an object from a Provider.

```csharp
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;

DefaultProviderLookup providerLookup = new();

Contact contact = (Contact)new RecordProvider(typeof(Contact), providerLookup)
    .Supply();
```

This creates a single `Contact`.

By default:

- one object is generated
- no records are persisted
- no related records are generated
- default values are supplied automatically

The returned object is immediately ready for use in your test.

---

# Providers

A Provider is responsible for generating test data for a particular record
type.

For example:

- a `Contact` Provider knows how to create Contacts
- an `Account` Provider knows how to create Accounts
- a `Case` Provider knows how to create Cases

Tests never need to know *how* these objects are constructed. They simply
request the object type they need.

Internally, Providers use centrally-defined Master Templates to populate
required fields and relationships.

---

# Provider Lookups

A Provider only knows *what* type of object you want.

A Provider Lookup knows *which Provider* should be used to generate it.

```csharp
DefaultProviderLookup providerLookup = new();

RecordProvider provider = new(typeof(Contact), providerLookup);
```

Separating Providers from Provider Lookups lets an application register
different Provider implementations without modifying the framework itself.

`DefaultProviderLookup` (`Net.Nowhereatall.Xfty.Demo`) is this port's own
starter-kit lookup — a working example to copy and adjust for your project, not
a base class to extend. See [extend/provider-lookups](../extend/provider-lookups.md).

---

# Override Templates

Most tests only care about one or two fields.

Instead of constructing an entire record, provide an Override Template
containing only the values relevant to your test.

```csharp
Contact contact = (Contact)new RecordProvider(typeof(Contact), providerLookup)
    .SetOverrideTemplate(new Contact { FirstName = "Alice", LastName = "Smith" })
    .Supply();
```

XFTY preserves the supplied values while generating everything else
automatically.

For example, if the Master Template specifies a default email address, that
value will still be generated.

If the Override Template specifies an email address, the Override Template
always wins.

---

# Shorthand Constructors

Three constructor overloads save a call for the most common starting points:

```csharp
// from a template - derives the record type (and any Provider variant) from it
new RecordProvider(new Contact { FirstName = "Alice" }, providerLookup);

// from a list of templates - derives the record type from the first
new RecordProvider(new List<object> { new Contact(), new Contact() }, providerLookup);

// from a lookup key - derives the record type from the key and pins that variant
new RecordProvider(LookupKey.Get(typeof(Contact)), providerLookup);
```

They are exactly equivalent to the `(Type, lookup)` constructor followed
by `SetOverrideTemplate(...)` / `SetOverrideTemplateList(...)` / `WithVariant(...)`.
Lookup keys and variants are covered in [provider-variants](provider-variants.md).

---

# Generating Multiple Records

There are two ways to create multiple records.

The simplest is to specify a quantity.

```csharp
List<object> contacts = new RecordProvider(typeof(Contact), providerLookup)
    .SetQuantityPerTemplate(5)
    .SupplyList();
```

This generates five Contacts using the same template.

If each generated record should differ, use an Override Template List instead.

```csharp
List<object> contacts = new RecordProvider(typeof(Contact), providerLookup)
    .SetOverrideTemplateList([
        new Contact { FirstName = "Alice" },
        new Contact { FirstName = "Bob" },
    ])
    .SupplyList();
```

When both a quantity and an Override Template List are supplied, every
template is generated the requested number of times.

---

# Creating Related Records

Relationship generation is controlled independently from persistence.

```csharp
Bundle bundle = new RecordProvider(typeof(Contact), providerLookup)
    .SetInsertMode(InsertMode.Mock)
    .SetInclusivity(InsertInclusivity.Required)
    .SupplyBundle();
```

The resulting Bundle contains both the requested Contacts and any related
records generated during the operation.

```csharp
object contact = bundle.GetList<Contact>(x => x.Id)![0];
object account = bundle.GetList<Contact>(x => x.AccountId)![0];
```

```text
Bundle
├── Contact
└── Account
```

The generated Contact automatically references the generated Account.

---

# Understanding Bundles

Bundles are the primary data structure returned by XFTY.

Rather than returning only the requested records, Bundles contain the entire
object graph created during generation.

For example, generating a `Case` may also generate:

```text
Case
├── Account
└── Contact
```

Bundles make every generated object available without requiring additional
lookups.

Lists are extracted using the relationship field that produced them.

```csharp
List<object> accounts = bundle.GetList<Case>(x => x.AccountId)!;
```

Nested Bundles can also be traversed.

```csharp
Bundle? accountBundle = bundle.GetBundle<Case>(x => x.AccountId);
```

---

# Insert Modes

Generating objects and persisting objects are separate concerns.

XFTY supports six insert modes.

| Mode | Description |
|------|-------------|
| `Never` | Generate records without Ids. |
| `Mock` | Generate realistic-looking Ids without any persistence. |
| `RelatedOnly` | Mock-Id only related records. |
| `Now` | Insert every generated record. **Always throws in this port — no persistence layer.** |
| `Later` | Behaves like `Never` while documenting that insertion will happen later. |
| `Deferred` | Generate like `Never` over many calls, registering everything for a single later flush. Flushing to real persistence also throws in this port; see [deferred-insert](deferred-insert.md). |

For most tests today:

| Test type | Recommended mode |
|------------|-----------------|
| Unit Test | `Mock` |

Because generated mock Ids do not point at real records, tests should never
treat a `Mock` record as if it were persisted.

---

# Relationship Inclusivity

Relationship generation is controlled independently from insertion.

| Mode | Description |
|------|-------------|
| `None` | Create no related records. |
| `Required` | Create only required relationships. |
| `All` | Create required and optional relationships. |
| `PreventCascade` | Create only the first level of relationships. |

`Required` is recommended for most tests.

It produces enough related data for records to be valid without generating
unnecessary object graphs.

---

# Which Supply Method Should I Use?

Every Provider ultimately generates a Bundle.

The convenience methods simply extract data from that Bundle.

| Method | Returns |
|---------|---------|
| `Supply()` | First generated record |
| `SupplyList()` | Primary generated records |
| `SupplyBundle()` | Entire generated object graph |

If your test only needs the requested records, `Supply()` or `SupplyList()` are
usually sufficient.

If your test needs to inspect related records, use `SupplyBundle()`.

---

# Next Steps

Now that you understand the basic workflow, each feature has its own page — see
the [feature matrix](README.md).

- [override-templates](override-templates.md) · [value-expressions](value-expressions.md) · [context-aware-values](context-aware-values.md) — customizing generated data
- [relationships](relationships.md) · [per-call-relationships](per-call-relationships.md) · [shared-ancestors](shared-ancestors.md) · [bundles](bundles.md) — object graphs
- [insert-modes](insert-modes.md) · [deferred-insert](deferred-insert.md) — persistence
- [advanced/](advanced/) — combining features

To teach XFTY about a new record type, see [extend/providers](../extend/providers.md).
What carries over from the Apex original (and what doesn't) is in
[reference/salesforce-considerations](../reference/salesforce-considerations.md).

Runnable: `RecordProviderIntegrationTest`, `RecordFactoryTest`
