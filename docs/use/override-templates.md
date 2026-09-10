# Override Templates

The most common customization. An **override template** is a partially-populated
record whose values replace those the Master Template would generate. Only the
properties you set are overridden; everything else is still generated.

---

## The simplest case

```csharp
Contact result = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .SetOverrideTemplate(new Contact { FirstName = "Alice", LastName = "Smith" })
    .Supply();
```

If the Contact Provider normally generates
`FirstName = "Contact First Name 1"`, `LastName = "Contact Last Name 1"`,
`Email = "test.contact1@example.com"`, the result is `Alice` / `Smith` /
`test.contact1@example.com` — the email is still generated.

A single override template can go straight to the constructor, which derives the
record type (and any Provider variant) from it:

```csharp
await new RecordProvider(new Contact { FirstName = "Alice" }, lookup)
    .Supply();
```

See [generating-records → shorthand constructors](generating-records.md#shorthand-constructors).

---

## Precedence

Customization is applied in a fixed order:

```text
Master Template  →  Put(...)  →  Override Template
```

If more than one customization touches a field, **the override template wins.**

```csharp
.Put<Contact>(x => x.FirstName, new LiteralExpression("Generated"))
.SetOverrideTemplate(new Contact { FirstName = "Alice" })
// -> "Alice", not "Generated"
```

An override value also wins over a [context-aware expression](context-aware-values.md).

### The one exception: an "empty" value

An override template can't set a field to its **empty** value — `null` for a
string / object / `Nullable<T>`, `0` / `false` / `default` for a number,
`bool`, `Guid`, `DateTime`, or enum — when the Master Template has a default
for it. "Set to empty" is indistinguishable from "not set", so the default
still fills it. To force the empty value, use `Put(...)` /
`provider[x => x.Field] = value` (which *is* tracked), or remove the default
for that call — see [Removing values](#removing-values) below. Any *non-empty*
override value works normally.

---

## Override template vs `Put(...)`

| Use an override template when… | Use [`Put(...)`](value-expressions.md) when… |
|--------------------------------|--------------------------------------------|
| customizing one or two records | every generated record should differ |
| supplying an exact value | replacing the *generation expression* |
| making one test more readable | generating unique values, or customizing relationships |

Override templates describe **data**; `Put(...)` describes **generation**.

---

## Removing values

Sometimes the Master Template supplies a value a test deliberately does not want
— testing a validation rule, a required-field error, a partially populated
record.

```csharp
.RemoveFromMasterTemplate<Contact>(x => x.Email)
```

This removes the field's generation entirely, rather than replacing it with
another value. For relationships, use
[`ExcludeRelationship(...)`](per-call-relationships.md) instead.

See also: [generating-records](generating-records.md) · [value-expressions](value-expressions.md)

Runnable: `RecordProviderApiTest`, `RecordFactoryTest`
