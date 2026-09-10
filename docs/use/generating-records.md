# Generating Records

The three `Supply*()` methods and the ways to ask for more than one record.

---

## One record

```csharp
Contact result = await new RecordProvider<Contact>(lookup)
    .Supply();
```

By default: one record, not inserted, no related records, default values filled.
`RecordProvider<Contact>` returns a typed record; the non-generic
`new RecordProvider(typeof(Contact), lookup)` returns `object` from `Supply()`.

---

## What record types work

Any type XFTY can construct and then write field-by-field by reflection:

- **`class`** and **`record class`** — the everyday case, including
  `init`-only properties (reflection writes them anyway) and positional
  records.
- **`struct`** / **`record struct`** / **`readonly record struct`** — fine for
  a `Mock` or `Never` unit test.

Every field is populated *after* construction, so a type needs either a public
parameterless constructor or, failing that, XFTY falls back to an
uninitialised instance — a positional `record class Foo(...)` with no other
constructor still works.

The edges — a `struct` under `InsertMode.Now`, a type with only `{ get; }`
properties, `bundle.Inject(...)` over nested structs — are in
[reference/known-issues.md](../reference/known-issues.md).

---

## Which supply method?

Every Provider produces a [Bundle](bundles.md); the supply methods pull data out
of it.

| Method | Returns |
|--------|---------|
| `Supply()` | the first generated primary record (`Contact` from `RecordProvider<Contact>`, `object` from the non-generic) |
| `SupplyList()` | all primary records (`List<Contact>` / `List<object>`) |
| `SupplyBundle()` | the whole generated object graph (a `Bundle` either way) |

Use `Supply()` / `SupplyList()` when the test only needs the requested records;
`SupplyBundle()` when it needs related records too.

---

## Many copies of one template

```csharp
List<Contact> results = await new RecordProvider<Contact>(lookup)
    .SetQuantityPerTemplate(5)
    .SupplyList();
```

---

## Different values per record

```csharp
List<Contact> results = await new RecordProvider<Contact>(lookup)
    .SetOverrideTemplateList([
        new Contact { FirstName = "Alice" },
        new Contact { FirstName = "Bob" },
    ])
    .SupplyList();
```

Each template inherits its remaining values from the Master Template.

### Combining the two

`SetQuantityPerTemplate(2)` with a two-template list produces four records, and
quantity is applied **outside** the template loop:

```text
Alice, Bob, Alice, Bob        (not Alice, Alice, Bob, Bob)
```

---

## Shorthand constructors

Three overloads on the **non-generic** `RecordProvider` save a call when the
record type is already implied by what you pass:

```csharp
// from a template - derives the record type (and any Provider variant) from it
new RecordProvider(new Contact { FirstName = "Alice" }, lookup);

// from a list of templates - derives the record type from the first
new RecordProvider([new Contact(), new Contact()], lookup);

// from a lookup key - derives the record type from the key and pins that variant
new RecordProvider(LookupKey.Get<Contact>(), lookup);
```

Each is equivalent to `new RecordProvider<Contact>(lookup)` followed by
`SetOverrideTemplate(...)` / `SetOverrideTemplateList(...)` / `WithVariant(...)` —
use whichever reads better; the generic form keeps `Supply()` typed.
Lookup keys and variants: [provider-variants](provider-variants.md).

---

## Going further

This page is only the "how many, from what template" part. The rest of what a
Provider call can do, each on its own page:

| You want to… | Page |
|---|---|
| control the field values (expressions, overrides, precedence) | [value-expressions](value-expressions.md), [override-templates](override-templates.md) |
| derive a value from a sibling, an ancestor, or a child | [context-aware-values](context-aware-values.md) |
| generate the **parent** records a record needs (and how deep) | [relationships](relationships.md) |
| force or exclude a specific relationship for this one call | [per-call-relationships](per-call-relationships.md) |
| generate **child** records hanging below the primaries | [child-records](child-records.md) |
| share **one** parent across many generated records | [shared-ancestors](shared-ancestors.md) |
| pick a Provider variant (flavour key) | [provider-variants](provider-variants.md) |
| choose whether/when records are inserted (`Mock` / `Now` / …) | [insert-modes](insert-modes.md) |
| build a graph across several calls and insert it once | [deferred-insert](deferred-insert.md) |
| read every generated record back without a query | [bundles](bundles.md) |

Combinations of these are worked in [advanced/](advanced/).

See also: [override-templates](override-templates.md) · [insert-modes](insert-modes.md) · [bundles](bundles.md)

Runnable: `RecordProviderApiTest`, `RecordFactoryTest`
