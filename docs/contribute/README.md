# Contributing to XFTY

You are here to **work on XFTY itself** — the engine, its tests, packaging.

| Page | Covers |
|------|--------|
| [coding-standards](coding-standards.md) | The rules this port's code is held to — the review checklist. Read this first. |
| [architecture](architecture.md) | The generation pipeline, the phase classes, the generation context, the value passes, mock Ids, immutability — and *why* each is shaped that way. |
| [local-development](local-development.md) | `dotnet build` / `dotnet test`, the `.editorconfig`-enforced analyzers, measuring coverage. |
| [test-suites](test-suites.md) | How `Xfty.Test/` is organized, and the `Performance` trait. |
| [coverage-standards](coverage-standards.md) | "A consumer must never have to debug the framework"; line floor vs branch goal. |
| [packaging](packaging.md) | Project layout — `Xfty/` vs `Xfty.Test/` — and NuGet packaging status. |
| [ci](ci.md) | What the GitHub Actions workflow runs. |

For what is built / in progress / proposed, see [../roadmap/](../roadmap/).

House style is not optional — the full rules are in
[coding-standards](coding-standards.md), `.editorconfig` (analyzer-enforced at
build time), and `CSharp Style Rules.txt` at the repo root.
