# Continuous Integration

## `.github/workflows/ci.yml` — every push and PR to `master`

Two jobs, no secrets.

### `build-and-test` (`ubuntu-latest`)

```yaml
dotnet restore Xfty.ci-cross-platform.slnf
dotnet build Xfty.ci-cross-platform.slnf --no-restore                        # .editorconfig analyzers enforced - a style violation fails the build
dotnet format Xfty.slnx --verify-no-changes --severity info                  # whitespace + IDE1006 naming + IDE0130 namespace-folder, which the build itself does not run
dotnet test Xfty.ci-cross-platform.slnf --no-build --filter "Category!=Performance"   # the normal suite - must pass
dotnet test Xfty.Test/Xfty.Test.csproj --no-build --filter "Category=Performance"     # informational only (continue-on-error)
python3 scripts/verify-doc-examples.py                                       # every documented code example is exercised by a real test
python3 scripts/verify-doc-links.py                                          # every relative doc link and anchor resolves
```

`dotnet build` with `EnforceCodeStyleInBuild` runs most of `.editorconfig`, but
not the naming analyzer (IDE1006) or IDE0130 (namespace-matches-folder, which
only fires for `partial` types). `dotnet format --verify-no-changes` is the gate
for those two and for every whitespace/formatting rule; it runs against the raw
`Xfty.slnx` (it only reads source, so the `net472` project is not a problem
here). See [coding-standards](coding-standards.md#quality-gates).

Runs against [`Xfty.ci-cross-platform.slnf`](../../Xfty.ci-cross-platform.slnf)
(a solution filter: every project in `Xfty.slnx` except
`Xfty.NetStandardCompat.Test`), not the raw `.slnx` — that one project targets
`net472`, a .NET Framework binary, and Linux runners can't execute it at all.
See the `windows-net472` job below for that project's own coverage.

The performance step is deliberately scoped to `Xfty.Test/Xfty.Test.csproj`
directly rather than the solution filter — it's the only project with any
`Category=Performance` tests, and targeting the whole filter meant every
other project reported "zero tests ran" (a failure), which
`continue-on-error` silently swallowed right alongside any real performance
regression. Scoped this way, the step's outcome actually means something;
`continue-on-error` stays, since wall-clock-based assertions are expected to
be flaky across CI runners, which is what it's there to tolerate.

The normal-suite step is not persistence-free: `PersistenceGatewayTest` proves
`Now`/`.DepthBatched()` against a mocked gateway, and
`Xfty.EntityFrameworkCore.Test` proves the same against a real SQLite database
and (Docker is preinstalled on `ubuntu-latest`) a real, ephemeral Postgres
container via Testcontainers - no secrets or external service needed, since
the container is created and torn down within the job.

### `windows-net472` (`windows-latest`)

```yaml
dotnet restore Xfty.NetStandardCompat.Test/Xfty.NetStandardCompat.Test.csproj
dotnet build Xfty.NetStandardCompat.Test/Xfty.NetStandardCompat.Test.csproj --no-restore
dotnet test Xfty.NetStandardCompat.Test/Xfty.NetStandardCompat.Test.csproj --no-build
```

Every package in this solution multi-targets `netstandard2.0;net8.0;net10.0` -
except `Xfty.EntityFrameworkCore` (`net8.0;net10.0`, plus a netstandard2.0
build riding EF Core 3.1.x - the last EF Core line with any netstandard2.0
asset at all, see that package's own csproj comment) and
`Xfty.EntityFramework6` (`net461;netstandard2.1` instead - classic EF6 ships
no netstandard2.0 asset of its own at all; `net461`, not `net472`, because
that's the actual floor `Xfty.EntityFramework6`'s own `Xfty` dependency
allows, see that package's own csproj comment). `netstandard2.0` (and
`Xfty.EntityFramework6`'s classic `net461` asset, which has the same
"isn't itself runnable on this Linux runner" problem for a different reason)
isn't itself a runnable platform — it's a contract other real runtimes
implement — so proving a build actually *runs*, not just compiles, needs a
real runtime that implements that contract. `net472` is the one this repo
targets (one Framework generation above `net461`, so it resolves and runs
the same `net461` asset a `net461` host would), and it needs a Windows
runner, since .NET Framework binaries can't execute on Linux at all — hence
its own job, separate from `build-and-test`.

This project references a package's classic-runtime output for one of three
reasons:

- **A real netstandard2.0-only code branch of its own** — `Xfty` itself
  (`Xfty/Internal/CollectionCompatExtensions.cs`'s `GetValueOrDefault`/
  `ToHashSet`, and `Internal/SharedRandom.cs`) and `Xfty.VectorDatabases`
  (its own `<Compile Include>` of that same physical `SharedRandom.cs` file
  into its own assembly - a separate assembly has no access to `Xfty`'s
  internal type at the IL level, so compiling the file again is the actual
  fix, not a copy of it).
- **A real dependency pinned at an unusually old version to reach
  netstandard2.0 at all**, where "compiles" is a much weaker guarantee than
  usual — `Xfty.EntityFrameworkCore`'s EF Core 3.1.x pairing, proven with an
  actual SQLite round-trip (`SqliteNowPersistenceSmokeTest`), not assumed
  from a clean compile against a six-year-old package.
- **A package whose classic-.NET-Framework asset is a genuinely different
  physical build from its modern one, not a netstandard2.0 build at all** —
  `Xfty.EntityFramework6`'s `net461` asset, proven with its own real SQLite
  round-trip (`Ef6SmokeTest`); its `netstandard2.1` asset is already proven
  for real on `net10.0` by `Xfty.EntityFramework6.Test`, so this job's part
  is only the classic-Framework side.

Every other multi-targeted package (`Bogus`, `AutoBogus`, `AutoFixture`,
`Xunit`, the two vector-database preview packages) has none of the above —
current dependencies, no netstandard2.0-only branch, no separate classic
asset — so `dotnet build`'s ordinary per-TFM compile against
`Xfty.ci-cross-platform.slnf` in `build-and-test` (builds every
`TargetFrameworks` entry for every project in the filter, not just the one
the test host happens to run on) is already the whole of its proof; this job
has nothing to add for those.

There is no scratch-org provisioning, no scheduled second workflow, and no
long-lived environment either pipeline depends on or seeds.

See [coverage-standards](coverage-standards.md) for what "must pass" is
measured against, and
[reference/volume-and-limits](../reference/volume-and-limits.md) for what the
performance step checks.
