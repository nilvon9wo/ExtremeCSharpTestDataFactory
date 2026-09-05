# Packaging

XFTY is a standard .NET solution — no SFDX source format, no package
directories, no namespace.

```text
Xfty.slnx
Xfty/            - the library (Net.Nowhereatall.Xfty)
  Core/          - the public types (RecordProvider, Bundle, MasterTemplate, ...)
  Engine/        - the generation pipeline
  Persistence/   - Id assignment, deferred / depth-batched resolution
  Values/        - the bundled value expressions
  Relationships/ - DefaultRelationship, SharedAncestor
  Lookup/        - LookupKey, FlavouredLookupKey, ProviderLookups
  Predicates/    - the reusable IRecordPredicate conditions
  Demo/          - this port's own bundled Account/Contact Providers + demo record types
Xfty.Test/       - the xUnit test suite (Net.Nowhereatall.Xfty.Test), mirroring Xfty/'s folders
Xfty.EntityFrameworkCore/       - optional: IPersistenceGateway via EF Core
Xfty.EntityFrameworkCore.Test/  - proven against SQLite + a real Postgres container
Xfty.Bogus/                     - optional: realistic-value IValueExpressions wrapping Bogus
Xfty.Bogus.Test/
Xfty.VectorDatabases/           - optional: a random-vector IValueExpression
Xfty.VectorDatabases.Test/
Xfty.VectorDatabases.Qdrant/      - PREVIEW: a Qdrant IPersistenceGateway - see its own README first
Xfty.VectorDatabases.Qdrant.Test/
```

`Xfty.VectorDatabases.Qdrant` is deliberately not like the others - it's a
preview proof-of-concept (`0.1.0-preview.1`, not `1.0.0-beta.1`), not yet a
considered, general-purpose package. See
[roadmap/vector-databases.md](../roadmap/vector-databases.md) and the
package's own README before depending on it.

**Each class's test sits in the mirrored folder** — `Xfty/Core/Bundle.cs` and
`Xfty.Test/Core/BundleTest.cs`.

- Local development, `dotnet build`/`test`: [local-development](local-development.md)
- Test organization: [test-suites](test-suites.md)
- CI: [ci](ci.md)

---

## Consuming XFTY

`Xfty`, `Xfty.EntityFrameworkCore`, `Xfty.Bogus`, and `Xfty.VectorDatabases`
all carry NuGet package metadata (`PackageId`, `Version`, `Authors`,
`PackageLicenseExpression`, embedded `README.md`, symbol packages) and
`dotnet pack` produces a valid `.nupkg`/`.snupkg` pair for each — verified
locally, not yet published. Only `Xfty` is required; the other three are
independent, opt-in add-ons a project references only if it wants that
specific convenience (EF Core persistence, Bogus-backed realistic values, a
random-vector expression).

Until a version is pushed to nuget.org, consume XFTY the same way any
not-yet-published package is consumed:

```bash
dotnet add reference path/to/Xfty/Xfty.csproj
```

or build the packages yourself and add a local NuGet feed:

```bash
dotnet pack Xfty/Xfty.csproj -c Release -o ./local-packages
dotnet pack Xfty.EntityFrameworkCore/Xfty.EntityFrameworkCore.csproj -c Release -o ./local-packages
dotnet pack Xfty.Bogus/Xfty.Bogus.csproj -c Release -o ./local-packages
dotnet pack Xfty.VectorDatabases/Xfty.VectorDatabases.csproj -c Release -o ./local-packages
dotnet nuget add source ./local-packages -n xfty-local
```

`Xfty.VectorDatabases.Qdrant` also packs cleanly (`dotnet pack` verified),
but is deliberately excluded from the publish plan below - a `0.x-preview`
proof-of-concept isn't ready for a public nuget.org listing yet.

## Publishing to nuget.org

This is the one remaining step, and it needs the package owner's own
nuget.org account and API key — nothing about it can be scripted or done on
someone else's behalf:

```bash
dotnet nuget push ./local-packages/Xfty.<version>.nupkg \
  --api-key <your-nuget-api-key> \
  --source https://api.nuget.org/v3/index.json
```

`Xfty.EntityFrameworkCore` depends on the `Xfty` package id/version, so push
`Xfty` first; `Xfty.Bogus` and `Xfty.VectorDatabases` depend only on `Xfty`
too, so the same order works for all three. Once a version is live on
nuget.org, it's automatically searchable from Visual Studio's NuGet Package
Manager (VS searches nuget.org by default) — no separate listing step.

A repeatable alternative is a GitHub Actions step, triggered on tag push,
that runs `dotnet pack` then `dotnet nuget push` using a `NUGET_API_KEY`
repository secret — avoids re-typing the API key locally for every release,
at the cost of storing it as a secret instead.
