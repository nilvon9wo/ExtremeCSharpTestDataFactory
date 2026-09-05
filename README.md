# XFTY — Extreme C# Test Data Factory

[![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml)

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

- Declarative test data generation, described once per Provider
- Automatic relationship generation — required, optional, shared ancestors,
  self-referential cycles guarded automatically
- Context-aware values: a field derived from a sibling, a generated
  ancestor, or (once the graph exists) a generated child, with a loud error
  on a mis-ordered read instead of a silent wrong `null`
- Per-call relationship control (`IncludeOptional`, `ExcludeRelationship`)
  without touching a Provider's own definition
- Real persistence through `IPersistenceGateway` — `Xfty.EntityFrameworkCore`
  ships an EF Core implementation, proven against SQLite and a real Postgres
  container — or mock Ids with no database touched at all
- Optional add-on packages for two common conveniences core `Xfty` doesn't
  bundle: `Xfty.Bogus` (realistic names/emails/addresses/paragraphs) and
  `Xfty.VectorDatabases` (a random-vector value expression for an embedding
  field) — neither is a dependency of core `Xfty` itself
- Deferred and depth-batched insert: build a graph across several calls, then
  insert it once, in dependency order, across mixed record types
- Multi-variant Providers (`FlavouredLookupKey`, `DiscriminatorLookupKey`) —
  resolve a different Provider for the same type by an arbitrary predicate or
  field value
- Lambda-based field access throughout (`x => x.Field`, not a bare
  `PropertyInfo` or `nameof(...)`)
- Extensible Provider architecture — implement `IRecordProvider` directly, or
  use `SimpleRecordProvider<T>` when a Provider is nothing but a template
- Suitable for both isolated unit tests and real-database integration tests,
  with the same Provider definitions

See [How XFTY compares](#how-xfty-compares) for how this stacks up against
AutoFixture, Bogus, and similar libraries.

---

# Quick Example

Generate a `Contact` with sensible defaults:

```csharp
DefaultProviderLookup lookup = new();

Contact contact = (Contact)new RecordProvider(typeof(Contact), lookup)
    .Supply();
```

Override only the fields your test actually cares about:

```csharp
Contact contact = (Contact)new RecordProvider(typeof(Contact), lookup)
    .Put<Contact>(x => x.FirstName, "Alice")
    .SetInsertMode(InsertMode.Mock)
    .Supply();
```

Generate complete related object graphs:

```csharp
Bundle bundle = new RecordProvider(typeof(Contact), lookup)
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
an optional add-on for that) and no auto-population - every field a
Provider cares about is declared, not guessed. See
[docs/reference/comparison.md](docs/reference/comparison.md) for the full,
unvarnished comparison against AutoFixture, Bogus, AutoBogus, and NBuilder,
including where XFTY is a worse fit than any of them.

---

# Roadmap

Recently landed (see the [CHANGELOG](CHANGELOG.md) for the full detail,
including everything since the 1.0.0-beta.1 tag):

- Real persistence via `IPersistenceGateway` (`Xfty.EntityFrameworkCore`,
  proven against SQLite and a real Postgres container)
- `DiscriminatorLookupKey` — resolving a Provider by a field's value
- Lambda-based field access across the whole public API
- A full sweep to a from-scratch, idiomatic C# port with no remaining
  Salesforce-specific surface
- `Xfty.Bogus` and `Xfty.VectorDatabases` — optional packages for realistic
  fake data and vector-embedding fields, without adding either dependency to
  core `Xfty`

The full status table — built, not-ported, and open ideas under
consideration (embedded/denormalized document relationships, an
AutoFixture-backed auto-population fallback) — is
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
