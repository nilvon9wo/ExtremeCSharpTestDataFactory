# Local Development

This is a standard .NET solution — `Xfty.slnx`, two projects (`Xfty/`, the
library; `Xfty.Test/`, the xUnit test suite). No org, no CLI beyond `dotnet`,
no scratch environment of any kind to provision.

Prerequisites: the [.NET SDK](https://dotnet.microsoft.com/download) matching
`net10.0` (see `Xfty/Xfty.csproj`).

---

## The loop

```bash
dotnet restore Xfty.slnx
dotnet build Xfty.slnx                                              # .editorconfig analyzers enforced - a style violation fails the build
dotnet test Xfty.slnx --filter "Category!=Performance"              # the normal suite
dotnet test Xfty.slnx --filter "Category=Performance"                # the informational performance suite (see test-suites.md)
```

Run a single test class or method with xUnit's standard filter syntax:

```bash
dotnet test Xfty.slnx --filter "FullyQualifiedName~ContextAwareExpressionTest"
```

**A note on stability:** because this port's `SharedAncestor` and
`DeferredInserter` statics do not reset between test methods (see
[reference/salesforce-considerations](../reference/salesforce-considerations.md)),
run the full suite (not just the file you touched) at least a couple of times
before trusting a passing result on shared-ancestor or deferred-insert changes
— a leak from one test into another shows up as an intermittent, order-
dependent failure rather than a deterministic one.

---

## Measuring coverage

```bash
dotnet test Xfty.slnx --collect:"XPlat Code Coverage"
```

`coverlet.collector` is already referenced in `Xfty.Test.csproj`. Feed the
resulting `coverage.cobertura.xml` to `reportgenerator` (or your IDE's
built-in coverage view) for a readable report. See
[coverage-standards](coverage-standards.md) for the bar this is measured
against.

---

## Smart App Control (Windows)

If `dotnet test` intermittently fails or hangs for no code-related reason on a
Windows machine, check the Windows Event Log's CodeIntegrity channel for a
Smart App Control block before assuming a code bug — it has been observed to
intermittently interfere with the test host process on some machines.
