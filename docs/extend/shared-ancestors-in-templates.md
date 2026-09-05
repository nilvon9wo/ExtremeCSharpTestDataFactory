# Shared Ancestors in a Master Template

A relationship slot normally holds a `DefaultRelationship`, which generates a
fresh parent per child. To make a relationship point at **one shared record**
instead, put a `SharedAncestor` in the same slot:

```csharp
new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
    .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get("primary-account"));
```

`SharedAncestor` implements the relationship interface (`IDefaultRelationship`),
so `PutRequired` / `PutOptional` accept it unchanged. Configure it once,
centrally — the same way a project defines its
[flavoured lookup keys](provider-variants.md):

```csharp
SharedAncestor.Put("primary-account", new Account { Name = "Primary" });
```

### Flat vs deep — nothing to opt into

The template reference (`SharedAncestor.Get("name")`) and the central config
are the same however heavy the shared record is:

```csharp
// flat - a plain parent; resolves as a single shared record
SharedAncestor.Put("primary-account", new Account { Name = "Primary" });

// deep - a record that pulls in ancestors of its own; resolves as a
// depth-batched sub-graph, built once
SharedAncestor.Put("root", new Account { Name = "Global HQ" });
SharedAncestor.Put("region", new Account { Name = "Region HQ" })
    .PutRequired(Field.Of<Account>(nameof(Account.ParentId)), SharedAncestor.Get("root"));
```

XFTY decides which by inspecting the ancestor's Provider's Master Template.

**Ship the default with the lookup, not the test.** A Provider that references
a shared ancestor should work out of the box: put the default on the lookup
that ships alongside it — the `ProviderLookups.Of(providerMap, defaults)`
overload, or implement `ISharedAncestorDefaults` on a hand-written lookup and
call `SharedAncestor.PutIfAbsent(...)` in its
`RegisterSharedAncestorDefaults()`. See
[use/shared-ancestors → Packaged defaults](../use/shared-ancestors.md#packaged-defaults).
A test still overrides by registering its own record first.

---

## When to put it in a *shipped* Provider

Only when the shared parent is genuinely part of the model — a singleton config
record, a project-wide root. For a test-specific "these all share one account",
it is clearer to set it on the `RecordProvider` instance in that test with
`.PutRequired(...)`.

Full behaviour, configuration, and current limits:
[use/shared-ancestors](../use/shared-ancestors.md).
