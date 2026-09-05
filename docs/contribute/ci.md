# Continuous Integration

## `.github/workflows/ci.yml` — every push and PR to `master`

One job, six steps, no secrets:

```yaml
dotnet restore Xfty.slnx
dotnet build Xfty.slnx --no-restore                                          # .editorconfig analyzers enforced - a style violation fails the build
dotnet test Xfty.slnx --no-build --filter "Category!=Performance"            # the normal suite - must pass
dotnet test Xfty.slnx --no-build --filter "Category=Performance"             # informational only (continue-on-error)
python3 scripts/verify-doc-examples.py                                       # every documented code example is exercised by a real test
python3 scripts/verify-doc-links.py                                          # every relative doc link and anchor resolves
```

The normal-suite step is not persistence-free: `PersistenceGatewayTest` proves
`Now`/`.DepthBatched()` against a mocked gateway, and
`Xfty.EntityFrameworkCore.Test` proves the same against a real SQLite database
and (Docker is preinstalled on `ubuntu-latest`) a real, ephemeral Postgres
container via Testcontainers - no secrets or external service needed, since
the container is created and torn down within the job.

There is no scratch-org provisioning, no scheduled second workflow, and no
long-lived environment this pipeline depends on or seeds.

See [coverage-standards](coverage-standards.md) for what "must pass" is
measured against, and
[reference/volume-and-limits](../reference/volume-and-limits.md) for what the
performance step checks.
