# Test-User Helpers — Not Provided

There is no bundled Provider exposing ready-made test-user helpers - an
admin-equivalent inserted user, or role/profile-style lookups - because there
is no role/profile schema here for such a lookup to resolve against. This
port's demo `User` (`Net.Nowhereatall.Xfty.Demo.User`) is deliberately
minimal (`Id`, `FirstName`, `LastName`, `Email`, `ManagerId`) so it can
exercise deep/hierarchical relationship paths without needing any of that -
see [reference/known-issues.md](../reference/known-issues.md).

Generating a plain `User` record for a test that just needs *some* user
reference works exactly like any other type:

```csharp
User someUser = (User)new RecordProvider(typeof(User), lookup)
    .SetInsertMode(InsertMode.Mock)
    .Supply();
```

See also: [extend/bundled-providers](../extend/bundled-providers.md)
