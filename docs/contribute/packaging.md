# Packaging

XFTY is a standard .NET solution — no SFDX source format, no package
directories, no namespace.

```text
Xfty.slnx
Xfty/            - the library (Net.NowhereAtAll.Xfty)
  Core/          - the public types (RecordProvider, Bundle, MasterTemplate, ...)
  Engine/        - the generation pipeline
  Persistence/   - Id assignment, deferred / depth-batched resolution
  Values/        - the bundled value expressions
  Relationships/ - DefaultRelationship, SharedAncestor
  Lookup/        - LookupKey, FlavouredLookupKey, ProviderLookups
  Predicates/    - the reusable IRecordPredicate conditions
  Demo/          - this port's own bundled Account/Contact Providers + demo record types
Xfty.Test/       - the xUnit test suite (Net.NowhereAtAll.Xfty.Test), mirroring Xfty/'s folders
Xfty.NetStandardCompat.Test/    - proves the netstandard2.0-only compatibility polyfills actually run (net472 - see ci.md)
Xfty.EntityFrameworkCore/       - optional: IPersistenceGateway via EF Core
Xfty.EntityFrameworkCore.Test/  - proven against SQLite + a real Postgres container
Xfty.Bogus/                     - optional: realistic-value IValueExpressions wrapping Bogus
Xfty.Bogus.Test/
Xfty.VectorDatabases/           - optional: a random-vector IValueExpression
Xfty.VectorDatabases.Test/
Xfty.VectorDatabases.Qdrant/      - PREVIEW: a Qdrant IPersistenceGateway via Qdrant's own client - see its own README first
Xfty.VectorDatabases.Qdrant.Test/
Xfty.VectorDatabases.MicrosoftExtensionsVectorData/      - PREVIEW: a generic IPersistenceGateway for any Microsoft.Extensions.VectorData connector
Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test/
Xfty.Xunit/                     - optional: [IsolatesSharedAncestor] xUnit attribute
Xfty.Xunit.Test/
Xfty.AutoFixture/               - optional: pairs XFTY with AutoFixture, both directions
Xfty.AutoFixture.Test/
Xfty.AutoBogus/                 - optional: pairs XFTY with AutoBogus, both directions
Xfty.AutoBogus.Test/
Xfty.FSharpAsync/               - optional: Async<'T> wrappers for F# code on the original async { } workflow
Xfty.FSharpAsync.Test/
```

`Xfty.VectorDatabases.Qdrant` and `Xfty.VectorDatabases.MicrosoftExtensionsVectorData`
are deliberately not like the others - both are preview proofs-of-concept
(`0.x-preview`, not `1.0.0-beta.1`), not yet considered, general-purpose
packages, and kept in **separate** packages from each other on purpose (see
[roadmap/vector-databases.md](../roadmap/vector-databases.md#why-two-packages-not-one)
for why combining them wasn't worth it even during this comparison phase).
Read each package's own README before depending on either.

**Each class's test sits in the mirrored folder** — `Xfty/Core/Bundle.cs` and
`Xfty.Test/Core/BundleTest.cs`.

- Local development, `dotnet build`/`test`: [local-development](local-development.md)
- Test organization: [test-suites](test-suites.md)
- CI: [ci](ci.md)

---

## Consuming XFTY

`Xfty`, `Xfty.EntityFrameworkCore`, `Xfty.Bogus`, `Xfty.VectorDatabases`,
`Xfty.Xunit`, `Xfty.AutoFixture`, `Xfty.AutoBogus`, and `Xfty.FSharpAsync`
are all published on nuget.org. Only `Xfty` is required; the other seven are
independent, opt-in add-ons a project references only if it wants that
specific convenience (EF Core persistence, Bogus-backed realistic values, a
random-vector expression, the `[IsolatesSharedAncestor]` xUnit attribute,
pairing with AutoFixture, pairing with AutoBogus, F#'s `Async<'T>`).

```bash
dotnet add package Xfty
```

Add whichever opt-in packages you want the same way (`dotnet add package
Xfty.EntityFrameworkCore`, etc.). `Xfty.VectorDatabases.Qdrant` and
`Xfty.VectorDatabases.MicrosoftExtensionsVectorData` are published too, as
`0.x-preview` versions rather than `1.0.0-beta.*` like the rest — each one's
own README is explicit about being a proof-of-concept with named, accepted
limitations rather than a considered general-purpose package; read it before
depending on either.

Working against an unpublished local change instead (contributing to XFTY
itself, or trying something before it's released) uses the same pattern as
any not-yet-published package:

```bash
dotnet add reference path/to/Xfty/Xfty.csproj
```

or build the packages yourself and add a local NuGet feed:

```bash
dotnet pack Xfty/Xfty.csproj -c Release -o ./local-packages
dotnet nuget add source ./local-packages -n xfty-local
```

## Publishing to nuget.org

Already set up and in active use — currently at `1.0.0-beta.5` for the
eight non-preview packages and `0.1.0-preview.3` for the two vector-database
preview ones, across several releases so far (see
[CHANGELOG.md](../../CHANGELOG.md) for what shipped in each). Cutting a new
one needs the package owner's own
nuget.org account — nothing about it can be scripted or done on someone
else's behalf. Once a version is live, it's automatically searchable from
Visual Studio's NuGet Package Manager (VS searches nuget.org by default) — no
separate listing step. `Xfty.EntityFrameworkCore` depends on the `Xfty`
package id/version, so `Xfty` has to land first; `Xfty.Bogus`,
`Xfty.VectorDatabases`, `Xfty.Xunit`, `Xfty.AutoFixture`, `Xfty.AutoBogus`,
`Xfty.FSharpAsync`, and the two vector-database preview packages all depend
only on `Xfty` too, so the same order works for all ten.

### CI: Trusted Publishing — no stored secret at all

[`.github/workflows/publish.yml`](../../.github/workflows/publish.yml) — push
a `v*` tag, or run it manually from the Actions tab — builds, tests, packs
every publishable package including the two preview ones, and pushes all of
them with `--skip-duplicate`. It uses nuget.org's
[Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing):
the workflow requests a GitHub OIDC token (`permissions: id-token: write`),
`NuGet/login@v1` exchanges it for a NuGet API key that lives for one hour and
never touches a GitHub secret, and that key is what `dotnet nuget push` uses.
No `NUGET_API_KEY` (or any other long-lived credential) is stored in this
repo at all — nuget.org itself now recommends Trusted Publishing over a
stored key for any supported CI/CD workflow, GitHub Actions included.

One-time setup on nuget.org (repo owner only, under the username menu's
**Trusted Publishing** page): a policy pointing at this repo and at
`publish.yml`, scoped to the glob `Xfty*` rather than one entry per package -
**every publishable package in this solution is named with that prefix
specifically so a new one is covered automatically**; naming a new
publishable package anything else means either renaming it or widening the
policy before `publish.yml` can push it. Push access, not unlist/relist. A
repository **variable** (not a secret - it's a username, not a credential)
named `NUGET_USERNAME` holds the nuget.org profile name the login step logs
in as.

### One-off manual push from the command line — still needs an API key

nuget.org's own guidance: API keys "continue to work" for this case, just not
as the recommended choice for CI anymore.

```bash
dotnet nuget push ./local-packages/Xfty.<version>.nupkg \
  --api-key <your-nuget-api-key> \
  --source https://api.nuget.org/v3/index.json
```

Generate a key under username menu → **API Keys** → Create, the same place
Trusted Publishing lives (right next to it in the nuget.org UI).
