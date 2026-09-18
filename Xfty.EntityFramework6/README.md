# Xfty.EntityFramework6

[![NuGet](https://img.shields.io/nuget/v/Xfty.EntityFramework6.svg)](https://www.nuget.org/packages/Xfty.EntityFramework6/)

A database-backed `IPersistenceGateway` for
[`Xfty`](https://www.nuget.org/packages/Xfty), for a project still on classic
Entity Framework 6 (`System.Data.Entity.DbContext`, the `EntityFramework`
NuGet package) rather than EF Core - see
[`Xfty.EntityFrameworkCore`](https://www.nuget.org/packages/Xfty.EntityFrameworkCore)
for that one instead. The two `DbContext` types are unrelated, so this is a
separate implementation, not a variant of the EF Core one - install whichever
matches what your project actually references.

```bash
dotnet add package Xfty.EntityFramework6
```

## Usage

```csharp
using Net.NowhereAtAll.Xfty.EntityFramework6;

Contact contact = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .SetPersistenceGateway(new Ef6PersistenceGateway(dbContext))
    .SetInsertMode(InsertMode.Now)
    .Supply();

// contact is a real row, inserted through dbContext.SaveChangesAsync() -
// including its required Account, inserted first.
```

A string-typed Id left unset is filled with a fresh GUID before insert - the
common shape for a string primary key, which EF6 has no built-in generator
for (an integer identity column is left untouched; EF already populates that
on its own after `SaveChangesAsync()`). One `SaveChangesAsync()` call per
depth-batched layer when used with `.DepthBatched()`.

## Target frameworks

`net461;netstandard2.1` - EF6 itself ships no `netstandard2.0` asset, so this
package doesn't either (see the package README of `Xfty.EntityFrameworkCore`
or the [roadmap](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/roadmap/README.md)
for the fuller multi-targeting story). `netstandard2.1` covers everything
modern (`net8.0`, `net10.0`, and anything else netstandard2.1+). The classic
side is `net461`, not `net472`: EF6 itself reaches back further still
(`net40`/`net45`), but this package's own dependency on `Xfty`
(`netstandard2.0`) can't be consumed below .NET Framework 4.6.1 at all -
that's the real floor, not EF6's - so `net461` is targeted directly rather
than a newer, more conservative Framework version. Any .NET Framework
4.6.1+ project can reference this package.

## Full documentation

- [Insert modes](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/insert-modes.md) - `Mock` vs `Now`, and what a configured `IPersistenceGateway` changes
- [Deferred insert](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/deferred-insert.md) - `.DepthBatched()`, dependency-ordered inserts across mixed record types
- [Unit vs. integration tests](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/advanced/unit-vs-integration.md) - the same Provider definitions serving both
- [Everything else `Xfty` does](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory#readme)
