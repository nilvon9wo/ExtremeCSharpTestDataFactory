# XFTY — Extreme C# Test Data Factory

[![CI](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml/badge.svg)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Xfty.svg)](https://www.nuget.org/packages/Xfty/)

XFTY is a declarative test data factory for C#.

Instead of manually constructing complete object graphs for every test, you describe only the values your test actually cares about. XFTY supplies sensible defaults, automatically creates related records, and can either mock persistence entirely or actually insert through a pluggable `IPersistenceGateway`.

The same Provider definitions can therefore be used in a pure in-memory unit test or a real database integration test.

## Installation

```bash
dotnet add package Xfty
```

`Xfty` is the core package. Everything else in the XFTY ecosystem is optional.

## Why XFTY?

As an application grows, so does the amount of code required simply to create valid test data.

A `Contact` might require an `Account`. Later, a validation rule might require additional `Account` fields. Eventually another related type becomes mandatory. Over time, hundreds or thousands of tests can end up duplicating nearly identical setup code.

XFTY centralizes that knowledge.

Providers define how valid records are constructed once. Individual tests then override only the fields that matter to the behavior being tested.

The result is test data that is:

* shorter to write
* easier to read
* easier to maintain
* more resilient to application changes

## Quick Example

Generate a `Contact` using its Provider's defaults:

```csharp
DefaultProviderLookup lookup = new();

Contact contact = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .Supply();
```

Override only the fields the test cares about:

```csharp
Contact contact = (Contact)await new RecordProvider(typeof(Contact), lookup)
    .Put<Contact>(x => x.FirstName, "Alice")
    .SetInsertMode(InsertMode.Mock)
    .Supply();
```

Generate a complete related object graph:

```csharp
Bundle bundle = await new RecordProvider(typeof(Contact), lookup)
    .SetInsertMode(InsertMode.Mock)
    .SetInclusivity(InsertInclusivity.All)
    .SupplyBundle();

Contact contact = (Contact)bundle.GetList<Contact>(x => x.Id)![0];
Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];

Assert.Equal(account.Id, contact.AccountId);
```

The important distinction is that the test does not have to know how to construct the complete `Contact` → `Account` graph. That knowledge belongs to the Providers.

## Core Features

### Declarative generation

Providers describe the data a particular record needs. Tests can then override individual fields without rebuilding the entire object.

```csharp
.Put<Contact>(x => x.FirstName, "Alice")
```

Field access is expressed with lambdas rather than raw `PropertyInfo` instances or `nameof(...)`.

### Automatic relationships

XFTY can generate related records automatically, including:

* required relationships
* optional relationships
* shared ancestors
* self-referential relationships with cycle protection

Relationship behavior can also be controlled for an individual generation call with `IncludeOptional` and `ExcludeRelationship`, without changing the Provider itself.

### Context-aware values

Values can depend on other generated records or fields in the graph.

A value can be derived from:

* a sibling
* a generated ancestor
* a generated child

XFTY detects an attempt to read a value before the corresponding part of the graph has been generated and reports the problem rather than silently producing an incorrect `null`.

### Persistence

XFTY separates test-data generation from persistence through `IPersistenceGateway`.

A Provider can therefore be used with:

* `InsertMode.Mock` for tests that should not touch a database
* a real persistence implementation for integration tests

The core package does not require a database provider.

The official EF Core implementation is available separately as `Xfty.EntityFrameworkCore`.

### Deferred and depth-batched persistence

A graph can be built across multiple calls and then persisted as a bundle.

XFTY handles dependency ordering across mixed record types rather than requiring test code to manually determine which records must be inserted first.

### Provider variants

The Provider architecture supports multiple definitions for the same record type.

`FlavouredLookupKey` and `DiscriminatorLookupKey` allow a different Provider to be selected according to a runtime predicate or field value.

### Async throughout

The public generation and persistence pipeline is asynchronous.

This allows the same model to work with persistence implementations backed by databases, network services, or other inherently asynchronous infrastructure.

## Persistence Is Optional

The core `Xfty` package does not contain a database-specific persistence implementation.

This is intentional.

You can use XFTY purely as an in-memory test-data factory, or add the persistence implementation appropriate for your integration tests.

For example:

**Xfty.EntityFrameworkCore**

EF Core persistence through `IPersistenceGateway`.

[NuGet](https://www.nuget.org/packages/Xfty.EntityFrameworkCore/) · [Documentation](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/tree/master/Xfty.EntityFrameworkCore)

Additional persistence integrations are being developed separately rather than being bundled into the core package.

## The XFTY Ecosystem

`Xfty` is the core of a collection of small, optional packages. They add capabilities without making the core package depend on unrelated libraries.

Some examples include:

| Package                                                                                                                                    | Purpose                                                                |
| ------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------- |
| [`Xfty.EntityFrameworkCore`](https://www.nuget.org/packages/Xfty.EntityFrameworkCore/)                                                     | EF Core persistence through `IPersistenceGateway`                      |
| [`Xfty.Bogus`](https://www.nuget.org/packages/Xfty.Bogus/)                                                                                 | Realistic fake-data value generation using Bogus                       |
| [`Xfty.VectorDatabases`](https://www.nuget.org/packages/Xfty.VectorDatabases/)                                                             | Random-vector value generation for embedding fields                    |
| [`Xfty.AutoFixture`](https://www.nuget.org/packages/Xfty.AutoFixture/)                                                                     | Integration between XFTY and AutoFixture                               |
| [`Xfty.AutoBogus`](https://www.nuget.org/packages/Xfty.AutoBogus/)                                                                         | Integration between XFTY and AutoBogus                                 |
| [`Xfty.Xunit`](https://www.nuget.org/packages/Xfty.Xunit/)                                                                                 | xUnit integration, including `[IsolatesSharedAncestor]`                |
| [`Xfty.FSharpAsync`](https://www.nuget.org/packages/Xfty.FSharpAsync/)                                                                     | `Async<'T>` wrappers for F# `async { }` workflows                      |
| [`Xfty.VectorDatabases.Qdrant`](https://www.nuget.org/packages/Xfty.VectorDatabases.Qdrant/)                                               | Preview Qdrant persistence integration                                 |
| [`Xfty.VectorDatabases.MicrosoftExtensionsVectorData`](https://www.nuget.org/packages/Xfty.VectorDatabases.MicrosoftExtensionsVectorData/) | Preview persistence through Microsoft.Extensions.VectorData connectors |

These packages are independent and opt-in. Installing `Xfty` does not pull the entire ecosystem into your application.

## XFTY vs. General-Purpose Fixture Libraries

XFTY is not intended to replace every test-data or fixture library.

Libraries such as AutoFixture and Bogus solve different problems particularly well.

XFTY's focus is the combination of **declarative Providers, related object graphs, and persistence**.

In particular, XFTY can:

* generate a related graph rather than treating each object independently
* generate required and optional relationships
* deduplicate shared ancestors
* guard against self-referential relationship cycles
* use context-aware values derived from the generated graph
* use the same Provider definitions for mocked and real persistence
* select Provider variants according to runtime keys or predicates

Core `Xfty` deliberately does **not** attempt to provide realistic fake-data generation or automatic population of every unspecified property.

Those capabilities are available through optional ecosystem packages such as `Xfty.Bogus`, `Xfty.AutoFixture`, and `Xfty.AutoBogus`.

For a detailed comparison with AutoFixture, Bogus, AutoBogus, and NBuilder, see the project's [comparison documentation](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/reference/comparison.md).

## Documentation

The full documentation lives in the repository:

* [Getting started](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/getting-started.md)
* [Using XFTY](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/tree/master/docs/use)
* [Extending XFTY](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/tree/master/docs/extend)
* [API reference and cheatsheet](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/tree/master/docs/reference)
* [Comparison with other test-data libraries](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/reference/comparison.md)
* [Known issues](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/reference/known-issues.md)
* [Architecture](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/contribute/architecture.md)
* [Roadmap](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/tree/master/docs/roadmap)

## Platform Support

The core package targets:

* `netstandard2.0`
* `net8.0`
* `net10.0`

The `netstandard2.0` target allows XFTY to be consumed by older .NET implementations, including .NET Framework 4.6.1+, Mono, Xamarin, and older .NET Core applications, while the `net8.0` and `net10.0` targets provide native builds for modern .NET applications.

## Design Philosophy

XFTY is built around a simple idea:

> Tests should describe only what makes them unique.

Everything else should be generated automatically.

Rather than scattering test-data construction throughout a test suite, XFTY moves knowledge about valid records and their relationships into reusable Providers.

The framework then constructs the object graph required by the test, leaving the test itself focused on the behavior it is supposed to verify.

## Contributing

Contributions, bug reports, feature requests, and discussions are welcome.

The project repository contains the source code, tests, documentation, roadmap, and contribution information:

[ExtremeCSharpTestDataFactory on GitHub](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory)

## License

XFTY is released under the MIT License.
