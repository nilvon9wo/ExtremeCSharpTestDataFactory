# Path-Scoped Value Overrides

Status: **✅ built** (`PathValue`, `Put(List<PropertyInfo>, value)` on
`RecordProvider`).

`IncludeOptional(List<PropertyInfo>)` walks a path of relationship fields into
the generated ancestors to force each step required, for one call.
`Put(path, value)` uses the same path mechanism to set **how a field on an
ancestor is generated**, for one call — without touching that ancestor's
Provider.

```csharp
await new RecordProvider(typeof(Contact), lookup)
    .SetInclusivity(InsertInclusivity.Required)
    .Put([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Industry)], "Aerospace")
    .Supply();
// -> the generated Account has Industry = "Aerospace"
```

`path` is `[rel1, rel2, ..., targetField]`.

## What "value" can be

Every kind plain `Put` / `PutRequired` / `PutOptional` accept:

| Call | Effect on the ancestor's field |
|---|---|
| `Put(path, object literal)` | a constant |
| `Put(path, IValueExpression)` | a value expression (runs once per generated ancestor) |
| `Put(path, IContextAwareExpression)` | evaluated against the ancestor as `RecordBeingBuilt` |
| `PutRequired(path, IDefaultRelationship)` | the ancestor's own lookup gets a generated parent |
| `PutOptional(path, IDefaultRelationship)` | …optional on the ancestor |

## Semantics

- **Forces its whole path, regardless of the call's inclusivity.** Every
  relationship named — the walk steps *and* a `PutRequired`/`PutOptional`
  target — is generated even at the default `None`. `IncludeOptional(...)`
  behaves the same way. A path field that is not a relationship on the
  Provider throws — never a silent no-op.
- **A forced ancestor is generated fully formed.** Its own required
  relationships still fill in, even at `None` inclusivity. Everything **not**
  on a forced path stays at the call's inclusivity.
- Threaded through `GenerationContext` next to `ForcedRelationshipPaths`;
  `ForRelated(field)` drops the head and carries the rest one level down.
- Applied by `PathValueApplier` onto a copy of the master template for the
  level being generated — after `RelationshipForcer`. A path `Put` on a field
  the ancestor's Provider already sets **wins**.

## Notes

- Shared ancestors: `Put(path, ...)` / `PutRequired(path, plainRelationship)`
  that would **set a value on** a shared ancestor **throws** — the shared
  record is resolved once and shared, so a per-call value has no well-defined
  meaning. Configure it with `SharedAncestor.Put(name, ...).Put(field, ...)`
  instead. `PutRequired(path, SharedAncestor.Get(name))` — wiring a shared
  ancestor **in** as an ancestor's relationship value — is fine.
