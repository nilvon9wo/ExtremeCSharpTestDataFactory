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
```

**Each class's test sits in the mirrored folder** — `Xfty/Core/Bundle.cs` and
`Xfty.Test/Core/BundleTest.cs`.

- Local development, `dotnet build`/`test`: [local-development](local-development.md)
- Test organization: [test-suites](test-suites.md)
- CI: [ci](ci.md)

---

## Consuming XFTY

Both `Xfty` and `Xfty.EntityFrameworkCore` carry NuGet package metadata
(`PackageId`, `Version`, `Authors`, `PackageLicenseExpression`, embedded
`README.md`, symbol packages) and `dotnet pack` produces a valid
`.nupkg`/`.snupkg` pair for each — verified locally, not yet published.

Until a version is pushed to nuget.org, consume XFTY the same way any
not-yet-published package is consumed:

```bash
dotnet add reference path/to/Xfty/Xfty.csproj
```

or build the packages yourself and add a local NuGet feed:

```bash
dotnet pack Xfty/Xfty.csproj -c Release -o ./local-packages
dotnet pack Xfty.EntityFrameworkCore/Xfty.EntityFrameworkCore.csproj -c Release -o ./local-packages
dotnet nuget add source ./local-packages -n xfty-local
```

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
`Xfty` first. Once a version is live on nuget.org, it's automatically
searchable from Visual Studio's NuGet Package Manager (VS searches
nuget.org by default) — no separate listing step.

A repeatable alternative is a GitHub Actions step, triggered on tag push,
that runs `dotnet pack` then `dotnet nuget push` using a `NUGET_API_KEY`
repository secret — avoids re-typing the API key locally for every release,
at the cost of storing it as a secret instead.
