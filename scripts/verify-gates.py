#!/usr/bin/env python3
"""
Prove every quality gate still catches what it is supposed to catch.

A gate that silently stops running looks exactly like a gate that passes -
which is how line length, unnecessary usings and coverage all went
unenforced here while the docs said otherwise. So:

* StyleCanary/ holds deliberately non-compliant code, each violation tagged
  `// expect: <gate>:<rule>` on the line a gate must report. Each static gate
  (build, format, layout, inspect) runs against it; every marker must be
  matched by a finding, and every gate must have at least one marker.
* The coverage gate is proven by adding a throwaway uncovered class to Xfty/
  - Xfty.Test must then fail (with no failing test: the threshold, not a
  test, is what failed).
* run-tests.py's INCONCLUSIVE verdict is proven by hiding one test module's
  apphost, so it cannot start - the way Smart App Control blocks one on
  Windows - and requiring that verdict instead of dotnet test's "Passed!".
* The doc gates are proven by adding a throwaway page under docs/use/ with a
  broken link and a `Runnable:` example no test exercises - both
  verify-doc-links.py and verify-doc-examples.py must then fail on it.

Every temporary file is removed again, however the run ends. Run after
`dotnet tool restore` (inspectcode). See StyleCanary/README.md.

Usage: verify-gates.py
Exits 1, naming every gate that missed something, if any gate is unproven.
"""
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CANARY_PROJECT = os.path.join("StyleCanary", "StyleCanary.csproj")
CANARY_SOURCE = os.path.join("StyleCanary", "Violations.cs")

EXPECT_MARKER = re.compile(r"// expect: (?P<gate>[\w-]+):(?P<rule>[\w.-]+)")
# `File.cs(line,col): error RULE` - MSBuild's diagnostic shape, which both
# `dotnet build` and `dotnet format` use. `info` counts: format fails on it.
MSBUILD_DIAGNOSTIC = re.compile(
    r"(?P<path>[^\s(:]+\.cs)\((?P<line>\d+),\d+\): (?:error|warning|info) (?P<rule>[A-Za-z]+\d*)"
)
# The build gate only reports what fails the build: errors. A compiler
# warning the canary expects (CS0649) therefore proves TreatWarningsAsErrors.
BUILD_ERROR = re.compile(r"(?P<path>[^\s(:]+\.cs)\((?P<line>\d+),\d+\): error (?P<rule>[A-Za-z]+\d*)")
# `path:line: rule: message` - the shape check-line-layout.py and
# inspect-code.py print.
COLON_DIAGNOSTIC = re.compile(r"^(?P<path>[^\s:]+\.cs):(?P<line>\d+): (?P<rule>[\w.-]+):", re.M)

UNCOVERED_PATH = os.path.join("Xfty", "GateCanaryUncovered.cs")
UNCOVERED_SOURCE = """namespace Net.NowhereAtAll.Xfty;

/// <summary>Written and removed by scripts/verify-gates.py - never tested, on purpose.</summary>
public static class GateCanaryUncovered
{
    public static int Answer() => 42;
}"""
UNSTARTABLE_PROJECT = os.path.join("Xfty.Xunit.Test", "Xfty.Xunit.Test.csproj")
UNSTARTABLE_APPHOST = os.path.join(
    "Xfty.Xunit.Test", "bin", "Debug", "net10.0",
    "Net.NowhereAtAll.Xfty.Xunit.Test" + (".exe" if os.name == "nt" else ""),
)
STALE_DOC_PATH = os.path.join("docs", "use", "gate-canary.md")
STALE_DOC_SOURCE = """# Gate canary

Written and removed by scripts/verify-gates.py.

Runnable: `GateCanaryTest`

See [a page that does not exist](gate-canary-no-such-page.md).

```csharp
GateCanaryNeverTested.NoTestEverCallsThis(42);
```
"""


def run(command: list) -> tuple:
    completed = subprocess.run(
        command, cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace"
    )
    return completed.returncode, completed.stdout + completed.stderr


def python_script(name: str, *arguments: str) -> list:
    return [sys.executable, os.path.join("scripts", name), *arguments]


def findings(pattern: re.Pattern, output: str) -> set:
    return {
        (os.path.basename(match["path"]), int(match["line"]), match["rule"])
        for match in pattern.finditer(output)
    }


STATIC_GATES = {
    "build": lambda: findings(
        BUILD_ERROR, run(["dotnet", "build", CANARY_PROJECT, "-nologo", "--no-incremental"])[1]
    ),
    "format": lambda: findings(
        MSBUILD_DIAGNOSTIC,
        run(["dotnet", "format", CANARY_PROJECT, "--verify-no-changes", "--severity", "info"])[1],
    ),
    "layout": lambda: findings(COLON_DIAGNOSTIC, run(python_script("check-line-layout.py", CANARY_SOURCE))[1]),
    "inspect": lambda: findings(COLON_DIAGNOSTIC, run(python_script("inspect-code.py", CANARY_PROJECT))[1]),
}


def expectations() -> list:
    with open(os.path.join(ROOT, CANARY_SOURCE), encoding="utf-8-sig") as fh:
        lines = fh.read().splitlines()
    return [
        (match["gate"], os.path.basename(CANARY_SOURCE), number, match["rule"])
        for number, line in enumerate(lines, start=1)
        for match in EXPECT_MARKER.finditer(line)
    ]


def verify_static_gates() -> bool:
    expected = expectations()
    reported = {gate: run_gate() for gate, run_gate in STATIC_GATES.items()}
    missed = [
        (gate, file_name, line, rule)
        for gate, file_name, line, rule in expected
        if (file_name, line, rule) not in reported.get(gate, set())
    ]
    unproven = sorted(set(STATIC_GATES) - {gate for gate, *_ in expected})
    for gate, file_name, line, rule in missed:
        print(f"[MISSED] {gate} did not report {rule} at {file_name}:{line}")
    for gate in unproven:
        print(f"[UNPROVEN] no StyleCanary marker exercises the '{gate}' gate")
    print(f"static gates: {len(expected) - len(missed)}/{len(expected)} expected findings reported")
    return not missed and not unproven


def with_temporary_file(path: str, content: str, check):
    absolute = os.path.join(ROOT, path)
    with open(absolute, "w", encoding="utf-8", newline="") as fh:
        fh.write(content)
    try:
        return check()
    finally:
        os.remove(absolute)


def coverage_gate_fails() -> bool:
    """Xfty.Test must fail - and on its threshold, with every test passing."""
    def check() -> bool:
        code, output = run(["dotnet", "test", "--project", os.path.join("Xfty.Test", "Xfty.Test.csproj")])
        ran_tests = re.search(r"total: [1-9]", output) is not None
        no_test_failed = re.search(r"failed: 0\b", output) is not None
        return code != 0 and ran_tests and no_test_failed

    caught = with_temporary_file(UNCOVERED_PATH, UNCOVERED_SOURCE, check)
    print(f"coverage gate caught an uncovered class: {caught}")
    return caught


def runner_reports_an_unstarted_module() -> bool:
    """With one module unable to start, run-tests.py must say INCONCLUSIVE, not pass."""
    build_code, _ = run(["dotnet", "build", UNSTARTABLE_PROJECT, "-nologo"])
    apphost = os.path.join(ROOT, UNSTARTABLE_APPHOST)
    hidden = apphost + ".gate-canary"
    if build_code != 0 or not os.path.exists(apphost):
        print(f"run-tests.py could not be checked: no apphost at {UNSTARTABLE_APPHOST}")
        return False
    os.rename(apphost, hidden)
    try:
        code, output = run(python_script("run-tests.py", "--project", UNSTARTABLE_PROJECT, "--no-build"))
    finally:
        os.rename(hidden, apphost)
    caught = code == 3 and "INCONCLUSIVE" in output
    print(f"run-tests.py reported a module that never started as INCONCLUSIVE: {caught}")
    return caught


def doc_gate_fails(script: str) -> bool:
    def check() -> bool:
        code, output = run(python_script(script))
        return code != 0 and "gate-canary" in output

    caught = with_temporary_file(STALE_DOC_PATH, STALE_DOC_SOURCE, check)
    print(f"{script} caught the canary page: {caught}")
    return caught


def main() -> int:
    results = {
        "static gates": verify_static_gates(),
        "coverage": coverage_gate_fails(),
        "inconclusive runs": runner_reports_an_unstarted_module(),
        "doc links": doc_gate_fails("verify-doc-links.py"),
        "doc examples": doc_gate_fails("verify-doc-examples.py"),
    }
    failed = [name for name, proven in results.items() if not proven]
    if failed:
        print(f"verify-gates: NOT PROVEN - {', '.join(failed)}", file=sys.stderr)
        return 1
    print("verify-gates: every gate caught its canary")
    return 0


if __name__ == "__main__":
    sys.exit(main())
