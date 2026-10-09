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
dotnet format Xfty.slnx --verify-no-changes --severity info         # the pre-push check: whitespace/formatting
python3 scripts/check-line-layout.py                                # 120-char ceiling + wrapped ')' on its own line
dotnet tool restore && python3 scripts/inspect-code.py              # ReSharper inspections: unnecessary usings and what else Roslyn misses
python3 scripts/run-tests.py --solution Xfty.slnx --filter "Category!=Performance"   # the normal suite (dotnet test + an INCONCLUSIVE verdict for modules that never ran)
python3 scripts/run-tests.py --project Xfty.Test/Xfty.Test.csproj -p:XftyCoverage=false --filter "Category=Performance"   # the informational performance suite (see test-suites.md)
```

`dotnet format --verify-no-changes`, `check-line-layout.py` and
`inspect-code.py` are the same gates CI runs (see [ci.md](ci.md)) and the ones
most easily forgotten locally — the build passes without them but CI will not.
`inspect-code.py` needs a prior `dotnet build` (it runs inspectcode with
`--no-build`; the script's docstring says why). `dotnet format Xfty.slnx` (no `--verify-no-changes`)
applies the fixes it can; IDE1006 naming it flags but cannot auto-fix, so those
are hand edits.

Run a single test class or method with xUnit's standard filter syntax:

```bash
python3 scripts/run-tests.py --solution Xfty.slnx -p:XftyCoverage=false --filter "FullyQualifiedName~ContextAwareExpressionTest"
```

`-p:XftyCoverage=false` because every test run otherwise enforces 100%
coverage, which one class can never reach - see
[Measuring coverage](#measuring-coverage).

**A note on stability:** because this port's `SharedAncestor` and
`DeferredInserter` statics do not reset between test methods (see
[reference/salesforce-considerations](../reference/salesforce-considerations.md)),
run the full suite (not just the file you touched) at least a couple of times
before trusting a passing result on shared-ancestor or deferred-insert changes
— a leak from one test into another shows up as an intermittent, order-
dependent failure rather than a deterministic one.

---

## Measuring coverage

There is nothing extra to run: every `dotnet test` of a `*.Test` project
measures its package with `coverlet.MTP` and fails below 100% line or branch
coverage (see [coverage-standards](coverage-standards.md)). The report - a
`coverage.cobertura.*.xml` in a `TestResults/` folder (git-ignored) - feeds
`reportgenerator` or an IDE's coverage view when you need to see *which*
line is missing.

Pass `-p:XftyCoverage=false` to `dotnet test` for any run that cannot cover
everything by design (a filter, one class). The two vector-database test
projects need Docker running to reach the bar.

---

## Smart App Control (Windows)

If `dotnet test` intermittently fails or hangs for no code-related reason on a
Windows machine, check the Windows Event Log's CodeIntegrity channel for a
Smart App Control block before assuming a code bug — it has been observed to
intermittently interfere with the test host process on some machines,
reporting `0x800711C7` ("An Application Control policy has blocked this
file") for a freshly built test DLL. Coverage instrumentation makes it more
frequent. A module blocked before it can even start is the worst case:
`dotnet test` still prints "Test run summary: Passed!" for the modules that
did run, so `scripts/run-tests.py` (a pass-through wrapper around
`dotnet test`, the one CI uses) ends such a run with
`Test run summary: INCONCLUSIVE - N test module(s) did not run: ...` and
exit code 3 instead. Running the suite from WSL avoids it - from a separate clone or
copy, since a WSL `dotnet restore` rewrites `obj/project.assets.json` with
Linux paths that Visual Studio on Windows then cannot use.
