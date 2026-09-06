# Value Expressions

An [override template](override-templates.md) replaces a generated *value*. A
**value expression** changes *how a value is generated* — for every record the
Provider produces.

---

## `Put(...)` an expression

```csharp
await new RecordProvider(typeof(Contact), lookup)
    .Put<Contact>(x => x.FirstName, new IncrementingStringExpression("Test Contact"))
    .SupplyBundle();
// -> "Test Contact 1", "Test Contact 2", "Test Contact 3", ...
```

---

## Implicit exact values

`Put(...)` also accepts a bare value — anything that is not already an
expression or a relationship is wrapped in `LiteralExpression` automatically.

```csharp
.Put<Account>(x => x.Type, "Customer")
.Put<Account>(x => x.NumberOfEmployees, 500)
```

is exactly

```csharp
.Put<Account>(x => x.Type, new LiteralExpression("Customer"))
.Put<Account>(x => x.NumberOfEmployees, new LiteralExpression(500))
```

This works both on a Provider's Master Template and on `RecordProvider` itself.

---

## The bundled expressions

| Expression | Produces |
|----------|----------|
| `LiteralExpression` | the same value every time |
| `IncrementingStringExpression` | `prefix` + an incrementing suffix |
| `UniqueStringExpression` | guaranteed-unique strings |
| `UniqueStringOfLengthExpression` | unique strings of a fixed length |
| `UniqueEmailExpression` | unique email addresses |
| `IncrementingDecimalExpression` | incrementing decimals |
| `UniqueAcrossRunsExpression` | a prefix/suffix wrapped around a value unique even across separate process runs |

All live in `Net.Nowhereatall.Xfty.Values`.

---

## Setting a value on a generated ancestor

`Put` (and `PutRequired` / `PutOptional`) also takes a **path** —
`[rel1, ..., relN, targetField]` (a `List<PropertyInfo>`) — to control how a
field on a *generated ancestor* is produced, for this one call, without editing
that ancestor's Provider.

The value is whatever the field forms accept — **not just an exact value**:

<!-- sketch -->
```csharp
await new RecordProvider(typeof(Contact), lookup)
    .SetInclusivity(InsertInclusivity.Required)

    // an exact value
    .Put([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Industry)], "Aerospace")

    // an expression - the generated Account gets a unique name
    .Put([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)],
         new UniqueStringExpression("Acct"))

    // a context-aware value - evaluated against that ancestor
    .Put([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Site)],
         CopyFromSiblingExpression.From<Account>(x => x.Name))

    // a relationship - give the ancestor its own generated parent
    .PutRequired([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)],
         SharedAncestor.Get("mr-smith"))

    .Supply();
```

`Put(path, ...)` **forces its whole path**, whatever the inclusivity — every
relationship named is generated even at the default `None`, and a forced
ancestor is generated fully formed (its own required relationships fill in).
Everything **not** on a named path stays at the call's inclusivity. A path field
that is not a relationship on the ancestor's Provider throws — never a silent
no-op. A path `Put` wins over a value the ancestor's Provider already sets.

You **cannot** `Put` a plain value *onto* a [shared ancestor](shared-ancestors.md)
— that throws; shape it where it is registered
(`SharedAncestor.Get("hq").Put(field, ...)`). You **can** point a forced
relationship at one (as the `mr-smith` line above).

This shares the path-walk with
[`IncludeOptional(path)`](per-call-relationships.md#reaching-deeper--a-path).

---

## Override template vs `Put(...)`

| Use an [override template](override-templates.md) when… | Use `Put(...)` when… |
|---------------------------------------------------------|----------------------|
| customizing one or two records | every generated record should differ |
| supplying an exact value | replacing the generation expression |
| making one test more readable | generating unique values, or customizing relationships |

Override templates describe **data**; `Put(...)` describes **generation**.

---

## Performance

An override template lets the Master Template generate a value that is then
replaced. When generating very large graphs, `Put(...)` can skip generating
values that will never be used. Most tests should prefer readability.

---

## Custom expressions

Anything with real logic is a small `IContextAwareExpression` (reads other
fields — see [context-aware-values](context-aware-values.md)) or a plain
`IValueExpression`. Shipping one as a reusable extension:
[extend/custom-value-expressions.md](../extend/custom-value-expressions.md).

See also: [override-templates](override-templates.md) · [context-aware-values](context-aware-values.md) · [per-call-relationships](per-call-relationships.md)

Runnable: `PathValueTest`, `RecordFactoryTest`
