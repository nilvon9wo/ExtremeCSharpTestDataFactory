# Coverage Standards

## The framework must never make a consumer debug it

A test that fails because of an XFTY bug should say so loudly. So:

- **Any error that can trace back to the framework is loud** — a clear
  `XftyConfigurationException` naming the misconfigured field / relationship /
  call and the fix, never a silent `null` or an opaque downstream exception.
  Example: `context.SiblingValue(field)` throws, naming both fields and the
  `Put` order, rather than returning a misleading `null`
  ([../use/context-aware-values.md](../use/context-aware-values.md)).
- **Accessors that can miss throw at the call site.** `SharedAncestor.GetId`
  throws rather than returning `null` for an unresolved name.

---

## 100% line and branch coverage, on every run

Every `*.Test` project measures the one package it tests and **fails below
100% line and 100% branch coverage** - every guard, every `switch` arm,
both sides of every ternary and null-coalescing operator. It is not a CI
extra: the threshold is part of every `dotnet test`, local or CI, wired once
in `Directory.Build.props`:

- `coverlet.MTP` (not `coverlet.collector`, a VSTest data collector that
  silently measures nothing under the Microsoft Testing Platform runner
  `global.json` selects).
- `--coverlet-include "[Net.NowhereAtAll.<Package>]*"`, derived from the
  test project's name (`Xfty.Bogus.Test` measures `Net.NowhereAtAll.Xfty.Bogus`),
  so neither the test assembly nor xunit's own DLLs drag the numbers down.
- `Xfty.NetStandardCompat.Test` is not measured: it is a net472 smoke test
  across several packages, not one package's unit suite.

**When the bar is hard to reach, the code is usually the problem.** A branch
no test can reach is dead code - remove it rather than cover it. A branch
that is reachable only through a public entry point with hand-built input
(a `Bundle` assembled without generation, a custom `IProviderLocating`)
still gets a test: consumers can do that too. And a reachable branch that
fails quietly - returns `null`, swallows a bad state - gets fixed to fail
loudly, per the section above, before it gets its test.

A run that cannot reach 100% by design - a filtered subset, one class, the
`Performance` suite on its own - opts out explicitly:

```bash
dotnet test --project Xfty.Test/Xfty.Test.csproj -p:XftyCoverage=false --filter "Category=Performance"
```

`Xfty.VectorDatabases.Qdrant.Test` and
`Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test` reach 100% only
with Docker running: their Testcontainers-backed tests skip without it,
and the skipped code then counts as uncovered.

Scenarios still worth explicit tests as the engine grows: many-level graphs,
circular relationships beyond `PreventCascade`, and the open items in
[../reference/known-issues.md](../reference/known-issues.md).

---

## Doc examples are exercised by real tests

`scripts/verify-doc-examples.py` checks every significant call in every
` ```csharp ` block on a page carrying a `Runnable:` marker against the test
corpus, in CI, on every push. See [ci](ci.md).
