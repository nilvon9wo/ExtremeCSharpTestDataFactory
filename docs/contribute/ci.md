# Continuous Integration

## `.github/workflows/ci.yml` — every push and PR to `master`

Two jobs, no secrets.

### `build-and-test` (`ubuntu-latest`)

```yaml
dotnet restore Xfty.ci-cross-platform.slnf
dotnet build Xfty.ci-cross-platform.slnf --no-restore                        # .editorconfig analyzers enforced - a style violation fails the build
dotnet test Xfty.ci-cross-platform.slnf --no-build --filter "Category!=Performance"   # the normal suite - must pass
dotnet test Xfty.Test/Xfty.Test.csproj --no-build --filter "Category=Performance"     # informational only (continue-on-error)
python3 scripts/verify-doc-examples.py                                       # every documented code example is exercised by a real test
python3 scripts/verify-doc-links.py                                          # every relative doc link and anchor resolves
```

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

Core `Xfty` multi-targets `netstandard2.0;net8.0;net10.0`. `netstandard2.0`
isn't itself a runnable platform — it's a contract other real runtimes
implement — so proving its three compatibility polyfills
(`Xfty/Internal/NetStandardCompat.cs`'s `GetValueOrDefault`/`ToHashSet`/
`SharedRandom`) actually *run*, not just compile, needs a real runtime that
implements that contract. `net472` is the one this repo targets, and it
needs a Windows runner, since .NET Framework binaries can't execute on
Linux at all — hence its own job, separate from `build-and-test`.

There is no scratch-org provisioning, no scheduled second workflow, and no
long-lived environment either pipeline depends on or seeds.

See [coverage-standards](coverage-standards.md) for what "must pass" is
measured against, and
[reference/volume-and-limits](../reference/volume-and-limits.md) for what the
performance step checks.
