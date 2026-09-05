# Test-User Helpers — Not Ported

Apex's `XFTY_DefaultUserDataProvider` exposed `TEST_ADMIN_USER` (an inserted
System Administrator `User` for `System.runAs(...)`) plus `profileIdFor(...)`
and `roleIdFor(...)` lookups against a live org's `Profile` / `UserRole`
records.

**None of this is ported.** There is no `System.runAs`, no `Profile`, no
`UserRole`, and no live org to query in a C# unit test — this port's demo
`User` (`Net.Nowhereatall.Xfty.Demo.User`) is deliberately minimal (`Id`,
`FirstName`, `LastName`, `Email`, `ManagerId`) precisely so it can exercise
deep/hierarchical relationship paths without needing any of that. A bundled
`XFTY_DefaultUserDataProvider` equivalent was never attempted for the same
reason — see [reference/known-issues.md](../reference/known-issues.md).

Generating a plain `User` record for a test that just needs *some* user
reference works exactly like any other type:

```csharp
User someUser = (User)new RecordProvider(typeof(User), lookup)
    .SetInsertMode(InsertMode.Mock)
    .Supply();
```

See also: [extend/bundled-providers](../extend/bundled-providers.md)
