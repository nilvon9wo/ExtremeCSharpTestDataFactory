# XFTY — Extreme C# Test Data Factory

[![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Xfty.svg)](https://www.nuget.org/packages/Xfty/)

XFTY is a declarative test data factory for C#.

Instead of manually constructing complete object graphs for every test, you
describe only the values your test actually cares about. XFTY supplies
sensible defaults, automatically creates related records, and either mocks
persistence entirely or actually inserts through a pluggable
`IPersistenceGateway` — the same Provider definitions serve a pure in-memory
unit test and a real database integration test.

By centralizing test data definitions, XFTY dramatically reduces boilerplate
and makes tests more resilient to changing validation rules, required
fields, and evolving business logic.

## Installation

```bash
dotnet add package Xfty
```

Add whichever opt-in packages you want the same way. Only `Xfty` itself is
required — everything else is independent and opt-in, grouped below by
what each one actually does:

### Core

| Package | What it does | NuGet | Tests |
|---|---|:-:|:-:|
| **Xfty** | Declarative generation, relationships, persistence seam | [![NuGet](https://img.shields.io/nuget/v/Xfty.svg)](https://www.nuget.org/packages/Xfty/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |

### Persistence — `IPersistenceGateway` implementations

| Package | What it does | NuGet | Tests |
|---|---|:-:|:-:|
| [**Xfty.EntityFrameworkCore**](Xfty.EntityFrameworkCore/README.md) | Real, database-backed persistence via EF Core | [![NuGet](https://img.shields.io/nuget/v/Xfty.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Xfty.EntityFrameworkCore/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |
| [**Xfty<wbr>.VectorDatabases<wbr>.Qdrant**](Xfty.VectorDatabases.Qdrant/README.md) 🧪 | PREVIEW: persistence via Qdrant's own client directly | [![NuGet](https://img.shields.io/nuget/v/Xfty.VectorDatabases.Qdrant.svg)](https://www.nuget.org/packages/Xfty.VectorDatabases.Qdrant/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |
| [**Xfty<wbr>.VectorDatabases<wbr>.MicrosoftExtensionsVectorData**](Xfty.VectorDatabases.MicrosoftExtensionsVectorData/README.md) 🧪 | PREVIEW: persistence via any Microsoft.Extensions.VectorData connector | [![NuGet](https://img.shields.io/nuget/v/Xfty.VectorDatabases.MicrosoftExtensionsVectorData.svg)](https://www.nuget.org/packages/Xfty.VectorDatabases.MicrosoftExtensionsVectorData/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |

### Value Generation — bundled `IValueExpression`s

| Package | What it does | NuGet | Tests |
|---|---|:-:|:-:|
| [**Xfty.Bogus**](Xfty.Bogus/README.md) | Realistic fake data - names, emails, addresses, paragraphs | [![NuGet](https://img.shields.io/nuget/v/Xfty.Bogus.svg)](https://www.nuget.org/packages/Xfty.Bogus/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |
| [**Xfty.VectorDatabases**](Xfty.VectorDatabases/README.md) | A random-vector value expression for an embedding field | [![NuGet](https://img.shields.io/nuget/v/Xfty.VectorDatabases.svg)](https://www.nuget.org/packages/Xfty.VectorDatabases/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |

### Auto-Population Pairings

| Package | What it does | NuGet | Tests |
|---|---|:-:|:-:|
| [**Xfty.AutoFixture**](Xfty.AutoFixture/README.md) | Pairs XFTY with AutoFixture, both directions | [![NuGet](https://img.shields.io/nuget/v/Xfty.AutoFixture.svg)](https://www.nuget.org/packages/Xfty.AutoFixture/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |
| [**Xfty.AutoBogus**](Xfty.AutoBogus/README.md) | Pairs XFTY with AutoBogus, both directions | [![NuGet](https://img.shields.io/nuget/v/Xfty.AutoBogus.svg)](https://www.nuget.org/packages/Xfty.AutoBogus/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |

### Test-Framework & Language Integration

| Package | What it does | NuGet | Tests |
|---|---|:-:|:-:|
| [**Xfty.Xunit**](Xfty.Xunit/README.md) | `[IsolatesSharedAncestor]` xUnit attribute | [![NuGet](https://img.shields.io/nuget/v/Xfty.Xunit.svg)](https://www.nuget.org/packages/Xfty.Xunit/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |
| [**Xfty.FSharpAsync**](Xfty.FSharpAsync/README.md) | `Async<'T>` wrappers for F#'s original `async { }` workflow | [![NuGet](https://img.shields.io/nuget/v/Xfty.FSharpAsync.svg)](https://www.nuget.org/packages/Xfty.FSharpAsync/) | [![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml) |

🧪 = preview proof-of-concept, versioned `0.x-preview` rather than
`1.0.0-beta.*` - read its own README before depending on it for anything
beyond the question it was built to answer.

Every "Tests" badge above points at the same single CI workflow — this
repo builds and tests every package together, not each in isolation, so
there's no per-package signal distinct from the whole solution's.

---

# Why XFTY?

As a project grows, so does the amount of code required simply to create
valid test data.

A `Contact` requires an `Account`. Later, a validation rule requires
additional `Account` fields. Eventually another related type becomes
mandatory. Over time, hundreds or even thousands of tests can end up
duplicating nearly identical setup code.

XFTY centralizes that knowledge.

Instead of every test knowing how to construct a valid object graph,
Providers define that logic once, allowing individual tests to override only
the fields they actually care about.

The result is test code that is:

- shorter
- easier to read
- easier to maintain
- more resilient to application changes

---

# Features

## Core Generation

- Declarative test data generation, described once per Provider
- Automatic relationship generation — required, optional, shared ancestors,
  self-referential cycles guarded automatically
- Context-aware values: a field derived from a sibling, a generated
  ancestor, or (once the graph exists) a generated child, with a loud error
  on a mis-ordered read instead of a silent wrong `null`
- Per-call relationship control (`IncludeOptional`, `ExcludeRelationship`)
  without touching a Provider's own definition
- Lambda-based field access throughout (`x => x.Field`, not a bare
  `PropertyInfo` or `nameof(...)`)

## Persistence

- Real persistence through `IPersistenceGateway` — `Xfty.EntityFrameworkCore`
  ships an EF Core implementation, proven against SQLite and a real Postgres
  container — or mock Ids with no database touched at all
- Deferred and depth-batched insert: build a graph across several calls, then
  insert it once, in dependency order, across mixed record types
- Suitable for both isolated unit tests and real-database integration tests,
  with the same Provider definitions

## Provider Architecture

- Extensible Provider architecture — implement `IRecordProvider` directly, or
  use `SimpleRecordProvider<T>` when a Provider is nothing but a template
- Multi-variant Providers (`FlavouredLookupKey`, `DiscriminatorLookupKey`) —
  resolve a different Provider for the same type by an arbitrary predicate or
  field value

## Packages & Platform Support

- Optional add-on packages for common conveniences core `Xfty` doesn't
  bundle - none is a dependency of core `Xfty` itself. See the
  [package table](#installation) up top for the full roster.
- Targets `netstandard2.0`/`net8.0`/`net10.0` — .NET Framework 4.6.1+,
  Mono/Xamarin, and older .NET Core all work, not just current .NET

See [How XFTY compares](#how-xfty-compares) for how this stacks up against
AutoFixture, Bogus, and similar libraries.

---

# Quick Example

Generate a `Contact` with sensible defaults:

```csharp
DefaultProviderLookup lookup = new();

Contact contact = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .Supply();
```

Override only the fields your test actually cares about:

```csharp
Contact contact = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .Put<Contact>(x => x.FirstName, "Alice")
    .SetInsertMode(InsertMode.Mock)
    .Supply();
```

Generate complete related object graphs:

```csharp
Bundle bundle = await new RecordProvider(typeof(Contact), lookup)
    .SetInsertMode(InsertMode.Mock)
    .SetInclusivity(InsertInclusivity.All)
    .SupplyBundle();

Contact contact = (Contact)bundle.GetList<Contact>(x => x.Id)![0];
Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];

Assert.Equal(account.Id, contact.AccountId);
```

---

# Documentation

Full documentation is in [`docs/`](docs/README.md), organised by audience:

| I want to… | Go to |
|------------|-------|
| **Use XFTY to write tests** | [docs/use/](docs/use/) — start with [getting-started](docs/use/getting-started.md) |
| **Teach XFTY about my own record types** | [docs/extend/](docs/extend/) |
| **Work on XFTY itself** | [docs/contribute/](docs/contribute/) — [architecture](docs/contribute/architecture.md) |
| **Look something up** | [docs/reference/](docs/reference/) — [api-cheatsheet](docs/reference/api-cheatsheet.md), [known-issues](docs/reference/known-issues.md) |
| **See what's built / planned** | [docs/roadmap/](docs/roadmap/README.md) |

---

# Design Philosophy

XFTY was designed around a simple idea:

> Tests should describe only what makes them unique.

Everything else should be generated automatically.

Rather than scattering test data throughout an entire codebase, XFTY moves
that knowledge into reusable Providers that declaratively describe valid
records and their relationships.

The framework then constructs those object graphs automatically, allowing
test code to remain focused on the behaviour being tested rather than on
setup.

---

# How XFTY Compares

XFTY is not a general-purpose "fill in an object" library like AutoFixture,
and it doesn't ship realistic fake-data generators like Bogus. What it does
that they don't:

- Generates a related **graph** — required/optional relationships, shared
  ancestors deduplicated across many children, self-referential cycles
  guarded automatically — not one object at a time.
- Has an actual opinion about **persistence**: the same Provider definitions
  run as a pure in-memory `Mock` in a unit test, or insert for real through
  `IPersistenceGateway` in an integration test, with no rewrite.
- Resolves a different Provider **variant** for the same type by a runtime
  key or predicate, and supports **context-aware values** — a field derived
  from a sibling, ancestor, or generated child, with a loud guard against
  reading one that hasn't been generated yet.

Core `Xfty` has no built-in realistic fake-data generation (`Xfty.Bogus` is
an optional add-on for that) and no auto-population by default - every
field a Provider cares about is declared, not guessed. `Xfty.AutoFixture`
and `Xfty.AutoBogus` are optional pairings for the fields a Provider
*doesn't* care about (or for pointing the tool's own generation at a
Provider directly) - neither changes core `Xfty`'s own philosophy. See
[docs/reference/comparison.md](docs/reference/comparison.md) for the full,
unvarnished comparison against AutoFixture, Bogus, AutoBogus, and NBuilder,
including where XFTY is a worse fit than any of them.

---

# Roadmap

Recently landed (see the [CHANGELOG](CHANGELOG.md) for the full detail,
including everything since the 1.0.0-beta.1 tag):

## New Packages

- `Xfty.Bogus` and `Xfty.VectorDatabases` — optional packages for realistic
  fake data and vector-embedding fields, without adding either dependency to
  core `Xfty`
- `Xfty.AutoFixture` — pairs XFTY with AutoFixture both directions: point
  `fixture.Create<T>()` at a registered `RecordProvider`, and/or let
  AutoFixture fill in whatever fields a Provider's Master Template left
  unset
- `Xfty.AutoBogus` — the same pairing for AutoBogus (AutoFixture-style
  auto-population plus Bogus's realistic generators), completing the
  trifecta: XFTY now pairs with AutoFixture, Bogus, and AutoBogus
- `Xfty.Xunit` — `[IsolatesSharedAncestor]`, resetting `SharedAncestor`
  before/after a test class or method automatically
- `Xfty.FSharpAsync` — `Async<'T>` wrappers for F# code still built on
  `async { }` rather than the newer `task { }`, which needs no wrapper at all

## Core Engine

- `DiscriminatorLookupKey` — resolving a Provider by a field's value
- Lambda-based field access across the whole public API
- Typed `RecordProvider<TRecord>`/`ChildProvider<TChild>` wrappers — no cast
  at the `Supply()` call site, plus a `MasterTemplate<TRecord>`-style
  object-initializer indexer
- Real persistence via `IPersistenceGateway` (`Xfty.EntityFrameworkCore`,
  proven against SQLite and a real Postgres container)
- Persistence is fully `async` end to end — every `Supply`/`SupplyList`/
  `SupplyBundle` call, and everything reachable from it, is now genuinely
  `Task`-based, matching how every real backing store (EF Core, a vector
  database client, a network call) already works underneath

## Platform & Reliability

- A full sweep to a from-scratch, idiomatic C# port with no remaining
  Salesforce-specific surface
- A real, fixed thread-safety issue in `SharedAncestor` under concurrent
  test execution (xUnit's default; this repo's own suite had opted out)
- Core `Xfty` now multi-targets `netstandard2.0;net8.0;net10.0`, reaching
  .NET Framework 4.6.1+/Mono/Xamarin as well as modern .NET, verified via a
  dedicated `net472` test project (`netstandard2.0` isn't itself runnable)

The full status table — built, not-ported, and open ideas under
consideration (embedded/denormalized document relationships) — is
[docs/roadmap/README.md](docs/roadmap/README.md).

---

# Contributing

Contributions, bug reports, feature requests, and discussions are welcome.

If you would like to contribute:

- Open an issue to discuss proposed changes.
- Keep Provider implementations declarative whenever possible.
- Preserve backwards compatibility unless a compelling reason exists not to.
- Prefer simplicity and readability over additional abstraction.

---

# License

This project is released under the MIT License.
