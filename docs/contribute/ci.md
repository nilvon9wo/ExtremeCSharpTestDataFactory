# Continuous Integration

## `.github/workflows/ci.yml` — every push and PR to `master`

One job, four steps, no secrets, no external org or service:

```yaml
dotnet restore Xfty.slnx
dotnet build Xfty.slnx --no-restore                                          # .editorconfig analyzers enforced - a style violation fails the build
dotnet test Xfty.slnx --no-build --filter "Category!=Performance"            # the normal suite - must pass
dotnet test Xfty.slnx --no-build --filter "Category=Performance"             # informational only (continue-on-error)
```

That's the whole pipeline. There is no scratch-org provisioning, no Dev Hub
secret, no scheduled second workflow — Apex's `full-suite.yml` existed to
periodically re-run against a fresh scratch org and surface config drift, a
concern with no analog here since there is no persistence layer or live
environment this port depends on yet.

See [coverage-standards](coverage-standards.md) for what "must pass" is
measured against, and
[reference/volume-and-limits](../reference/volume-and-limits.md) for what the
performance step checks.
