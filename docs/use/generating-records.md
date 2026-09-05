# Generating Records

The three `Supply*()` methods and the ways to ask for more than one record.

---

## One record

```csharp
Contact result = (Contact)new RecordProvider(typeof(Contact), lookup)
    .Supply();
```

By default: one record, not inserted, no related records, default values filled.

---

## Which supply method?

Every Provider produces a [Bundle](bundles.md); the supply methods pull data out
of it.

| Method | Returns |
|--------|---------|
| `Supply()` | the first generated primary record |
| `SupplyList()` | all primary records |
| `SupplyBundle()` | the whole generated object graph |

Use `Supply()` / `SupplyList()` when the test only needs the requested records;
`SupplyBundle()` when it needs related records too.

---

## Many copies of one template

```csharp
List<object> results = new RecordProvider(typeof(Contact), lookup)
    .SetQuantityPerTemplate(5)
    .SupplyList();
```

---

## Different values per record

```csharp
List<object> results = new RecordProvider(typeof(Contact), lookup)
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

Three overloads save a call for the common starting points:

```csharp
// from a template - derives the record type (and any Provider variant) from it
new RecordProvider(new Contact { FirstName = "Alice" }, lookup);

// from a list of templates - derives the record type from the first
new RecordProvider(new List<object> { new Contact(), new Contact() }, lookup);

// from a lookup key - derives the record type from the key and pins that variant
new RecordProvider(LookupKey.Get(typeof(Contact)), lookup);
```

They are exactly equivalent to the `(Type, lookup)` constructor followed by
`SetOverrideTemplate(...)` / `SetOverrideTemplateList(...)` / `WithVariant(...)`.
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
