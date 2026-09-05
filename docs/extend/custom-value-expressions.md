# Custom Value Expressions

XFTY ships the [plumbing, not a mini-expression-language](../use/value-expressions.md).
Anything with real logic is a small class you write. The bundled `*Expression`
classes are just implementations of the same interfaces — a boolean derived
from a birthdate, a code built by concatenating a parent's Id fragment, a status
that mirrors a child's stage: all of it is an ordinary class.

There are **three** interfaces, one per "how much of the graph does the value
need to see":

| Interface | The value depends on | Runs |
|---|---|---|
| `IValueExpression` | nothing but itself | first value pass |
| `IContextAwareExpression` | other fields on the same record (**siblings**), or a generated **ancestor** | second value pass, per record |
| `IDeferredExpression` | a generated **child / descendant** | when a deferred graph is flattened (`Deferred` / `.DepthBatched()` only) |

---

## A plain value expression — `IValueExpression`

One no-argument method:

<!-- sketch -->
```csharp
public sealed class NextWeekday : IValueExpression
{
    public object? Get()
    {
        DateTime candidate = DateTime.Today.AddDays(1);
        while (IsWeekend(candidate))
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }

    private static bool IsWeekend(DateTime day) =>
        day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
```

<!-- sketch -->
```csharp
.Put<Contact>(x => x.Birthdate, new NextWeekday())
```

Stateful expressions (incrementing, unique) are fine and common.

**Limitation:** `Get()` sees nothing else. If the value has to look at another
field, it is a context-aware value, not this.

---

## Reading a sibling — `IContextAwareExpression`

A **separate** interface (a context-aware value genuinely cannot produce
anything without a context, so it does not pretend to satisfy the no-argument
contract):

<!-- sketch -->
```csharp
public sealed class IsAdultFlag : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        DateTime? birthdate = (DateTime?)context.SiblingValue(Field.Of<Contact>(x => x.Birthdate));
        return birthdate is not null && birthdate.Value.AddYears(18) <= DateTime.Today;
    }
}
```

Read siblings with **`context.SiblingValue(field)`**, not
`field.GetValue(context.RecordBeingBuilt)`: the guarded accessor throws a clear
error if `field` is another context-aware value that has not been generated
yet, rather than returning a misleading `null`.

**Limitations:**

- Context-aware values are generated **in `Put` order**. `SiblingValue(x)` only
  works if `x` was `Put` before this field (or is a plain value / override —
  those are all done first). Reading a *later* context-aware sibling throws,
  naming both fields and the fix.
- Only fields **on this record** are siblings. A field on a parent is an
  ancestor read (below); a field on a child is a descendant read (below).

---

## Reading a generated ancestor — `IContextAwareExpression`

The context carries the graph generated so far. `context.BundleSoFar.GetList(relationshipField)`
is the parent for each primary, aligned 1:1 — pick this record's with
`context.RowIndex`:

<!-- sketch -->
```csharp
public sealed class AccountNamePlusCountry : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        Account? parentAccount = AncestorAccount(context);
        return parentAccount is null ? null : $"{parentAccount.Name} - {parentAccount.ShippingCountry}";
    }

    private static Account? AncestorAccount(GenerationContext context)
    {
        if (context.BundleSoFar is null || context.RowIndex < 0)
        {
            return null;
        }

        List<object>? accounts = context.BundleSoFar.GetList<Contact>(x => x.AccountId);
        return accounts is null || context.RowIndex >= accounts.Count
            ? null
            : (Account)accounts[context.RowIndex];
    }
}
```

`context.BundleSoFar.GetValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ShippingCountry)], context.RowIndex)`
does the same walk in one call — use it instead of a hand-written helper when
you only need a field, not the whole record. `CopyFromAncestorExpression` is
that walk wrapped as a ready-made context-aware value (multi-hop: each leading
field is a `GetBundle(...)` down, the last is the field to read). Reach for it
first; write your own only when the value is a transformation, not a straight
copy.

**Limitations:**

- The ancestor must actually have been **generated** — its relationship has to
  be covered by the call's [inclusivity](../use/relationships.md#inclusivity)
  (or forced with [`IncludeOptional(...)`](../use/per-call-relationships.md)).
  If it was not, `GetList(field)` is `null`; return `null`, do not throw.
- **You see the ancestor before it is persisted.** Its non-Id fields are fully
  generated and safe to read in any mode. Its **`Id`** is a consistent mock
  under `Mock`, and **`null` under `Never` / `Deferred`** — the value pass runs
  before a deferred graph flattens. (`Now` would give a real Id, but `Now`
  always throws in this port.) If a child needs the parent's real Id under
  `Deferred`, put it in the **lookup field** (normal relationship generation —
  the depth-batched resolution wires it); a context-aware value into any other
  field cannot get it.

---

## Reading a generated child / descendant — `IDeferredExpression`

A child does not exist when its parent is built, so an up-flowing value cannot
run in either in-line pass. It gets its own interface and runs when a deferred
graph is flattened, over the whole forest:

<!-- sketch -->
```csharp
public sealed class HasAnyWebOriginCase : IDeferredExpression
{
    public object? Get(DeferredGraph graph, int recordIndex) =>
        graph.ChildrenOf(recordIndex, Field.Of<Case>(x => x.AccountId))
            .Cast<Case>()
            .Any(childCase => childCase.Origin == "Web");
}
```

<!-- sketch -->
```csharp
.Put<Account>(x => x.Description, new HasAnyWebOriginCase())
```

`graph.ChildrenOf(recordIndex, childLookupField)` returns every generated
record that references this one through `childLookupField` — whether it is the
child that *requested* this parent, or a row from a `WithChildren(...)`
collection. `CopyFromDescendantExpression` is the straight-copy case of this.

**Limitations:**

- **`Deferred` (or `.DepthBatched()`) only.** A Provider carrying one of these
  in any other insert mode **throws** — the forest never exists otherwise.
- The value is filled when the graph is flattened
  (`DeferredInsertBuffer.Flatten(bundle)`, or a real flush — which throws in
  this port, see [use/deferred-insert](../use/deferred-insert.md)). Before
  that the field is `null`.
- Only **direct** children (`ChildrenOf` follows one parent link). Grandchildren
  are not walked; read them from a child's own deferred value if you need them.

---

## Testing

A custom expression earns a test the same way a [Provider](providers.md) does
— generate with it, assert the value. This port's own bundled expressions are
tested per-class in `Xfty.Test/Values/`, and
`Xfty.Test/Values/ContextAwareExpressionTest.cs` is a worked example of custom
sibling/ancestor expressions driven end to end through `RecordProvider`.

Runnable: `ContextAwareExpressionTest`, `CopyFromDescendantExpressionTest`
