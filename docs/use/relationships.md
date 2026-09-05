# Relationships

XFTY generates complete object graphs, not isolated records. A Provider describes
the relationships a record type has, and XFTY creates the related records
automatically when a test asks for them.

- **This page:** required vs optional relationships, inclusivity, cascading.
- [per-call-relationships](per-call-relationships.md): one-off exceptions
  (`IncludeOptional`, `ExcludeRelationship`).
- [shared-ancestors](shared-ancestors.md): many children under one parent.
- [bundles](bundles.md): reading the generated graph.
- Writing the relationship into a Provider is an *extend* task —
  [extend/providers.md](../extend/providers.md).

---

## Required vs optional

A relationship is defined with `DefaultRelationship` and placed in either the
**required** or the **optional** slot of the Master Template.

```csharp
.PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account()))
.PutOptional<Account>(x => x.OwnerId,   new DefaultRelationship(new User()))
```

The supplied record acts as an override template for the generated parent — its
remaining fields come from that parent's own Provider. (This is why a
relationship takes a record instance, not just a type.)

- **Required** relationships are generated whenever relationship generation
  includes required data. Use this only for relationships genuinely needed for
  valid test data.
- **Optional** relationships are generated only under `All` inclusivity. Prefer
  optional — every required relationship enlarges every generated graph.

Picking a Provider variant for the parent (flavours) —
[provider-variants](provider-variants.md).

---

## Inclusivity

Relationship generation is controlled independently of insertion, with one
setting per call:

| Mode | Behaviour |
|------|-----------|
| `None` | Generate no related records — every relationship is the test's responsibility. |
| `Required` | Generate only required relationships. **The recommended default.** |
| `All` | Generate required **and** optional relationships. Richer graphs; use sparingly. |
| `PreventCascade` | Generate the first level of relationships, but stop each generated parent from generating its own. |

```csharp
.SetInclusivity(InsertInclusivity.Required)
```

---

## Cascading

Relationship generation is recursive. A `Case` that requires a `Contact`, which
requires an `Account`, generates all three:

```text
Case
└── Contact
    └── Account
```

Each Provider is responsible only for its own type; together they produce the
whole graph.

### `PreventCascade`

Some models are circular — an `Account` with a primary `Contact` that has an
`Account`. `PreventCascade` lets the first Provider create its direct
relationships while every subsequently invoked Provider behaves as though
inclusivity were `None`:

```text
Account
└── Contact          (not Contact → Account → Contact → …)
```

Reducing graph size is a side effect; **stopping recursion is the point.**

### Self-referential relationships

`All` + a self-referential relationship (e.g. `Account.ParentId → Account`) would
recurse forever. XFTY generates **one level** and then throws a clear "cycle"
error if the same Provider would be generated again further up the graph. Options
for a genuine chain:

- **`PreventCascade`** — exactly one level, no recursion.
- **distinct per-level Providers** (different [lookup keys](provider-variants.md))
  — each level is a different Provider, so it is not a cycle and recurses freely.
- **`.AllowAncestorCycles()`** on the Provider — suppresses the guard when the
  chain terminates for another reason (or the guard is a false positive). You
  own the "does it terminate?" question.

---

## Performance

Every additional relationship increases object count and memory. Prefer
`Required` over `All`; keep required relationships minimal; use `PreventCascade`
for deep or circular trees; use `None` only when the test wants total control.
For large graphs, see [advanced/large-graphs](advanced/large-graphs.md).

Runnable: `RecordFactoryTest`, `AncestorCycleTest`, `AncestorCycleGuardTest`
