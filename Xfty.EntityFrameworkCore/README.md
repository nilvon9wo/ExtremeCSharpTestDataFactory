# Xfty.EntityFrameworkCore

[![NuGet](https://img.shields.io/nuget/v/Xfty.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Xfty.EntityFrameworkCore/)

The real, database-backed `IPersistenceGateway` for
[`Xfty`](https://www.nuget.org/packages/Xfty) - the piece that makes
`InsertMode.Now` and `.DepthBatched()` actually persist, proven against a
real Entity Framework Core `DbContext` (SQLite and a real Postgres
container) rather than a mock.

```bash
dotnet add package Xfty.EntityFrameworkCore
```

## Usage

```csharp
using Net.NowhereAtAll.Xfty.EntityFrameworkCore;

Contact contact = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .SetPersistenceGateway(new EfPersistenceGateway(dbContext))
    .SetInsertMode(InsertMode.Now)
    .Supply();

// contact is a real row, inserted through dbContext.SaveChangesAsync() -
// including its required Account, inserted first.
```

A string-typed Id left unset is filled with a fresh GUID before `Add` - the
common shape for a string primary key, which EF Core has no built-in
generator for (an integer identity column is left untouched; EF already
populates that on its own after `SaveChangesAsync()`). One `SaveChangesAsync()`
call per depth-batched layer when used with `.DepthBatched()`.

## Full documentation

- [Insert modes](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/insert-modes.md) - `Mock` vs `Now`, and what a configured `IPersistenceGateway` changes
- [Deferred insert](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/deferred-insert.md) - `.DepthBatched()`, dependency-ordered inserts across mixed record types
- [Unit vs. integration tests](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/advanced/unit-vs-integration.md) - the same Provider definitions serving both
- [Everything else `Xfty` does](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory#readme)
