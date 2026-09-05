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

## Consuming XFTY today

There is no published NuGet package yet. Reference the project directly:

```bash
dotnet add reference path/to/Xfty/Xfty.csproj
```

or add it as a git submodule / copy the source, the way the Apex original was
consumed by copying `force-app/` before packages existed.

Publishing a NuGet package (`dotnet pack`) is straightforward once this port
is ready to version and release — `Xfty.csproj` has no package metadata
(`PackageId`, `Version`, `Authors`, …) set yet, which is what that step would
add.
