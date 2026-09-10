# Mock Id Generators

Under [`InsertMode.Mock`](../use/insert-modes.md) XFTY assigns each record a
placeholder identifier instead of doing a real persistence round-trip. What
that identifier *looks like* is an `IMockIdGenerator` — swap it when your
project's keys aren't one of the built-in shapes, or aren't strings at all.

---

## The built-in generator

`DefaultMockIdGenerator` renders one process-wide incrementing sequence as the
Id field's own type:

| Id field type | Mocked value |
|---|---|
| `string` | `"mock-1"`, `"mock-2"`, … |
| `int` / `long` | `1`, `2`, … |
| `Guid` | a fresh `Guid` each time |
| anything else | throws `XftyConfigurationException` naming the type — supply your own |

It is what runs when nothing else is configured. A non-string primary key
works out of the box; an exotic one fails loudly with the fix in the message.

---

## Writing your own

One method. Keep any sequence state on the instance.

<!-- sketch -->
```csharp
public sealed class AccountIdGenerator : IMockIdGenerator
{
    private int count;

    // "A" + a running number + a unix-seconds stamp
    public object NextId(MockIdContext context) =>
        $"A{++this.count}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
}
```

`MockIdContext` carries `RecordType`, `IdField`, and the `Record` instance
itself — read the record's other fields if the Id shape depends on them.

---

## Declaring it

**Per record type — on the Master Template.** This is where the knowledge
belongs: the Provider that knows how a valid `Account` looks also knows how
its Ids are shaped.

<!-- sketch -->
```csharp
new MasterTemplate<Account>(x => x.Id)
    .Put(x => x.Name, new IncrementingStringExpression("Acme"))
    .WithMockIdGenerator(new AccountIdGenerator());
```

**Per call — on the `RecordProvider`**, exactly like overriding a field value
or a relationship. This overrides the template's generator for *this call's
own primary records only*; any generated ancestor keeps the generator its own
Master Template declares.

<!-- sketch -->
```csharp
await new RecordProvider<Account>(lookup)
    .SetMockIdGenerator(new AccountIdGenerator())
    .SetInsertMode(InsertMode.Mock)
    .Supply();
```

Resolution order at mock time: the per-call `SetMockIdGenerator` →
the Master Template's `WithMockIdGenerator` → `DefaultMockIdGenerator`.

---

## Scope

- The **key field is never assumed to be named `Id`** — it comes from the
  Provider's `PrimaryTargetField` (`new MasterTemplate<Ledger>(x => x.LedgerId)`),
  through the whole persistence path, including the depth-batched / deferred
  inserter.
- A **key already set on the template is kept** under `Mock` — the generator
  fills only an unset key, so `new Order { OrderRef = "known-1" }` stays
  `"known-1"`.
- `IMockIdGenerator` applies to `InsertMode.Mock` only (the depth-batched and
  deferred paths included — each type's `WithMockIdGenerator` is honoured
  there too). Under `InsertMode.Now` the real backing store assigns the
  identifier: the `EfPersistenceGateway` fills an empty **string** key with a
  GUID before `Add`, and leaves an integer identity column to the database.

---

## Testing

The built-in shapes, a per-type `WithMockIdGenerator`, a per-call
`SetMockIdGenerator`, the loud throw for an unsupported Id type, and an
ancestor keeping its own generator while a child call overrides — all driven
end to end through `RecordProvider` in `Xfty.Test/Persistence/`.
`FlavouredMockIdHierarchyTest` adds the same POCO taking a different generator
per Provider variant, down a deep multi-generation chain.

Runnable: `MockIdGeneratorTest`, `FlavouredMockIdHierarchyTest`
