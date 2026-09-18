# Design: A Persistence Gateway for Pre-Code-First EF (`ObjectContext`)

Status: **idea, not designed.** Reasoned through in conversation, not measured
against real code the way [netstandard1x-support.md](netstandard1x-support.md)
was - no probe project, no call-site count. Treat the specifics below as a
starting point, not a scoped plan.

---

## The gap

`Xfty.EntityFramework6` covers classic EF6 (`System.Data.Entity.DbContext`,
Code First). It does not cover the EF generations before that: EF 1.0 (.NET
Framework 3.5 SP1, 2008) through EF 4.0 (.NET Framework 4.0, 2010) shipped
*in the framework itself* - no NuGet package at all, since NuGet didn't exist
yet - built around `ObjectContext`/`ObjectSet<T>`, Database First/Model First
via EDMX, no Code First. (EF 4.1-5.0 *did* add `DbContext`/Code First via
NuGet, but that's not a real gap - see below.)

## Why `Xfty.EntityFramework6` doesn't already cover this

Two different reasons, not one:

- **EF 4.1-5.0 isn't actually a gap.** `Xfty.EntityFramework6` depends on
  `EntityFramework` `6.5.2` as a *minimum* version - standard NuGet
  resolution bumps a project still on EF5's `DbContext` up to `6.5.2`
  automatically when it adds this package, and that upgrade is safe: EF6 was
  an incremental evolution of EF5's `DbContext`/`DbSet<T>` API, not a
  rewrite. There's a harder reason it has to work this way, too -
  `SaveChangesAsync()`, the method `Ef6PersistenceGateway` actually calls,
  was introduced *in* EF6. It didn't exist on EF5's `DbContext` at all
  (async/await had only just landed in .NET 4.5/C# 5). A gateway that talks
  to EF5 specifically couldn't be async in the first place - nothing to
  build.
- **`ObjectContext` (EF 1.0-4.0) is a genuinely different API**, not a
  narrower version of the same one:
  - No `DbContext.Set(Type)` equivalent - `ObjectContext` exposes either
    `CreateObjectSet<T>()` (generic, compile-time-typed only - no path from
    a runtime-discovered `Type` the way `Ef6PersistenceGateway.AddOne`
    needs) or `AddObject(string entitySetName, object entity)` (a
    *string*-keyed entity-set name, not a `Type` - a real design question:
    where would that string come from for a record type XFTY only knows
    reflectively?).
  - **No async at all** - not missing from this one API, missing from the
    *language*. .NET Framework 4.0 (2010) predates C# 5/async-await (2012)
    entirely. `IPersistenceGateway.Insert` returns `Task`; a gateway here
    would need to wrap a genuinely synchronous `ObjectContext.SaveChanges()`
    in `Task.CompletedTask`, not await anything real.
  - **No NuGet package to depend on.** EF 1.0-4.0 shipped inside the .NET
    Framework assemblies themselves. A package here couldn't
    `<PackageReference Include="EntityFramework" Version="...">` the way
    both EF gateways do today - it would need a framework `<Reference>` to
    `System.Data.Entity` instead, tying the whole package to a specific
    .NET Framework version in a way neither existing gateway does.

## What a real design would need

Sketched, not committed to:

- A decision on the `AddObject(string, object)` vs `CreateObjectSet<T>()`
  question above - likely reflection over `ObjectContext.MetadataWorkspace`
  to resolve a `Type` to its entity-set name, mirroring how
  `Xfty.EntityFramework6.Test`'s own schema-creation workaround already
  reaches into EF6's metadata for a different reason (see that test's own
  docstring).
- Confirming exactly which .NET Framework version(s) `ObjectContext` is
  actually available on without a separate NuGet package, and what the
  resulting `TargetFrameworks` floor even looks like for a package built
  this way - not established here.
- A synchronous-wrapped `Insert` implementation, and a decision on whether
  that's acceptable to present through the same `Task`-returning
  `IPersistenceGateway` seam everything else uses, or whether it needs its
  own contract.

## Worth it?

No - more clearly than [netstandard1x-support.md](netstandard1x-support.md)'s
already-negative answer, for the same two reasons stacked together instead
of one:

- **The audience is narrower still.** Someone still on `ObjectContext` today
  hasn't just skipped EF Core - they've skipped the entire DbContext/Code
  First/async generation of classic EF too, over a decade of EF's own
  evolution that `Xfty.EntityFramework6` already reaches.
- **No signal of demand at all**, not even the qualified case
  `Xfty.EntityFramework6` had (a large, currently-active real-world
  `net461`/EF6 population, even without a specific request). This surfaced
  from "how far back could we go," the same way the netstandard1.x idea did.

See also: [roadmap/README.md](README.md),
[netstandard1x-support.md](netstandard1x-support.md).
