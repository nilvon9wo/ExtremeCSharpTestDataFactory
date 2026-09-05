# Per-Call Relationship Exceptions

[Inclusivity](relationships.md#inclusivity) is one setting for the whole call.
When a single test needs **one exception** — generate a particular optional
relationship, or skip one that would otherwise be generated — override it per
relationship on the `RecordProvider` instance.

---

## The simplest case

```csharp
new RecordProvider(typeof(Contact), lookup)
    .IncludeOptional(Field.Of<Account>(x => x.OwnerId))     // generate this optional one too
    .ExcludeRelationship(Field.Of<Contact>(x => x.AccountId));   // do not generate this one, even though it is required
```

- **`IncludeOptional(field)`** generates one named relationship for this call,
  **whatever the inclusivity** — including the default `None` — and generates it
  *fully formed* (its own required relationships fill in). Everything not named
  stays at the call's inclusivity. Throws during generation if `field` is not a
  relationship on the Provider it resolves to.
- **`ExcludeRelationship(field)`** makes one relationship — required or
  optional — non-existent for this call: not generated, not attached, not left
  as an orphan reference. Throws if `field` is not a relationship (use
  [`RemoveFromMasterTemplate(...)`](override-templates.md#removing-values) for
  plain value fields). `ExcludeRelationshipIfPresent(field)` is the same, but a
  no-op instead of throwing when `field` is not a relationship — useful when a
  shared helper excludes a field that only some Providers declare.

Both act only on the instance they are called on — a different Provider using the
same Master Template still generates the relationship. `IncludeOptional` is
applied to a per-call copy of the Master Template, so it is order-independent;
call `ExcludeRelationship` before any `Put(...)` (same ordering rule as
[`WithVariant`](provider-variants.md)).

---

## Reaching deeper — a path

`IncludeOptional` also takes a **path** of relationship fields
(`List<PropertyInfo>`), forcing every step for this call only:

```csharp
new RecordProvider(typeof(Contact), lookup)
    .IncludeOptional([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)])
    .SetInclusivity(InsertInclusivity.Required);
```

generates the Contact's Account (required anyway) **and** that Account's own
parent Account (optional), leaving everything else at `Required`. Each step must
be a relationship on the Provider it resolves to; an unknown step throws during
generation. Whether a step is a plain relationship or a
[shared ancestor](shared-ancestors.md) makes no difference.
`IncludeOptional(field)` is shorthand for the one-element path.

---

## Setting a *value* on a generated ancestor

The same path walk also sets **how a field on an ancestor is generated** —
`Put(path, value)`, where the value is an exact value, an expression, a
context-aware value, or a relationship. That is a value concern, so it lives
with the other `Put` forms:
[value-expressions → setting a value on a generated ancestor](value-expressions.md#setting-a-value-on-a-generated-ancestor).

See also: [relationships](relationships.md) · [provider-variants](provider-variants.md)

Runnable: `RecordFactoryTest`, `PathValueTest`
