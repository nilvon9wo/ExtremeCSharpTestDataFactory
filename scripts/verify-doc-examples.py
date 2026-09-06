#!/usr/bin/env python3
"""
Guarantees every ```csharp block in docs/use/** and docs/extend/** is backed by
a real test somewhere in Xfty.Test.

For each page carrying a `Runnable:` line, every significant fragment of every
```csharp block on that page (a `.Method(...)` call, a `new ClassName(...)`, a
`: IInterfaceName` implementation, a `ClassName.StaticMethod(...)`) must appear
- whitespace/case normalised - in one of the test classes that line names
(informational: naming the "expected home" of the proof), OR anywhere else in
the whole Xfty.Test corpus (a doc line may legitimately be proven by a shared
helper, a differently-named test class, or a test that moved).

Fragments are fine: the check is line-by-line, not block-by-block, so a doc can
show `.Put(field, expr)` on its own and it still has to exist in a test.

A fence immediately preceded by `<!-- sketch -->` is illustrative
project-specific code (a consumer's own record types, lookup-key classes) that
cannot run against this port's own demo Account/Contact/Case/User Providers -
exempt, same as the Apex original's convention.

This is a mechanical, line-oriented port of the Apex original's
verify-doc-examples.py (see git history) - same shape, same leniency, C#
syntax and this port's naming convention (PascalCase, no XFTY_ prefix, test
classes named <Thing>Test) instead of Apex's.

Exit non-zero (and print every miss) if any documented call is not exercised.
"""
import re
import sys
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent

# Only the audience-facing feature docs promise runnable examples. Everything
# else is explicitly out of scope - notably docs/articles/, the author's
# personal essays: their code snippets are illustrative prose, not framework
# API, and must never be checked against the test suite.
DOC_DIRS = [ROOT / "docs" / "use", ROOT / "docs" / "extend"]
EXCLUDE_DIRS = [ROOT / "docs" / "articles"]
# Every *.Test project, not just the core Xfty.Test - an add-on package's own
# docs page (autofixture.md, say) is proven by tests in ITS OWN Test project
# (Xfty.AutoFixture.Test), which previously wasn't scanned at all. Discovered,
# not hardcoded, so a new add-on package's Test project is covered automatically.
TEST_DIRS = sorted(p for p in ROOT.glob("*.Test") if p.is_dir())

RUNNABLE_RE = re.compile(r"Runnable:\s*(.+)$", re.M)
CLASS_RE = re.compile(r"`(\w+Test)`")
# A fence immediately preceded by `<!-- sketch -->` is illustrative project
# code (a consumer's own record types / lookup-key classes) and is exempt - it
# cannot run against the bundled Account / Contact / Case / User Providers.
CSHARP_BLOCK_RE = re.compile(r"(?:^|\n)(<!-- sketch -->\n)?```csharp\n(.*?)\n```", re.S)
SIGNIFICANT_RE = re.compile(
    r"(\.\w+(?:<[^\n<>]*>)?\([^\n]*\)"        # .Method(...) / .Method<T>(...)
    r"|new\s+[A-Z]\w*(?:<[^\n<>]*>)?\([^\n]*\)"  # new ClassName(...) / new ClassName<T>(...)
    r"|:\s*I[A-Z]\w*"                          # : IInterfaceName (this port's interfaces are all I-prefixed)
    r"|[A-Z]\w*\.[A-Z]\w*(?:<[^\n<>]*>)?\([^\n]*\))"  # ClassName.StaticMethod(...)
)


def norm(s: str) -> str:
    # whitespace-insensitive and case-insensitive: docs write the lookup
    # placeholder as `lookup`, the test corpus as a `Lookup` local - same
    # thing. Full-fragment matching keeps this from producing false positives.
    return re.sub(r"\s+", "", s).lower()


def load_test_sources():
    blobs = {}
    for d in TEST_DIRS:
        for p in d.rglob("*.cs"):
            blobs[p.stem] = norm(p.read_text(encoding="utf-8"))
    return blobs


def main() -> int:
    tests = load_test_sources()
    all_tests_blob = norm("".join(pathlib.Path(f).read_text(encoding="utf-8")
                                 for d in TEST_DIRS for f in d.rglob("*.cs")))
    misses = []
    checked_pages = 0
    checked_lines = 0

    for d in DOC_DIRS:
        for page in sorted(d.rglob("*.md")):
            if any(ex in page.parents for ex in EXCLUDE_DIRS):
                continue
            text = page.read_text(encoding="utf-8")
            runnable_lines = RUNNABLE_RE.findall(text)
            if not runnable_lines:
                continue
            checked_pages += 1
            named = [c for line in runnable_lines for c in CLASS_RE.findall(line)]
            # the union of every named test class's source, plus a fallback to
            # the whole test corpus (a doc line may legitimately be proven by a
            # shared helper class, or a test that moved)
            scope = "".join(tests.get(n, "") for n in named) or all_tests_blob
            for sketch_marker, block in CSHARP_BLOCK_RE.findall(text):
                if sketch_marker:
                    continue
                for line in block.splitlines():
                    line = line.strip()
                    if not line or line.startswith("//"):
                        continue
                    for frag in SIGNIFICANT_RE.findall(line):
                        checked_lines += 1
                        if norm(frag) not in scope and norm(frag) not in all_tests_blob:
                            misses.append(f"{page.relative_to(ROOT)}  |  {frag}"
                                          f"  |  not in {', '.join(named) or 'any test'}")

    print(f"checked {checked_lines} documented calls across {checked_pages} pages")
    if misses:
        print(f"\n{len(misses)} documented call(s) with no backing test:\n")
        print("\n".join("  " + x for x in misses))
        return 1
    print("every documented csharp call is exercised by a test")
    return 0


if __name__ == "__main__":
    sys.exit(main())
