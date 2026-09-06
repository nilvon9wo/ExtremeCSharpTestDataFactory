# Xfty.VectorDatabases.MicrosoftExtensionsVectorData

**Status: PREVIEW / proof-of-concept.** Versioned `0.x-preview`, not
`1.0.0-beta.1` like every other package in this repo, on purpose - see
[Why "preview" and not "beta"](#why-preview-and-not-beta) below.

## What it does

`MevdPersistenceGateway` implements XFTY's `IPersistenceGateway` by
inserting `RecordProvider`-generated records into
[`Microsoft.Extensions.VectorData`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.vectordata)'s
abstract `VectorStore` - **any** `VectorStore`. It depends on nothing but
`Microsoft.Extensions.VectorData.Abstractions`; whichever concrete
connector you construct the `VectorStore` from (Qdrant, Redis, Azure AI
Search, pgvector, SQLite, Weaviate, …) is entirely your choice, made
outside this package. This gateway has no idea, and no dependency on,
which one you picked. `GetDynamicCollection`, `EnsureCollectionExistsAsync`,
and `UpsertAsync` are declared on the abstract `VectorStore` base class
itself, so none of this is actually Qdrant-specific, despite Qdrant being
what this package's own test happens to use - see
[`Xfty.VectorDatabases.Qdrant`](../Xfty.VectorDatabases.Qdrant/README.md)
for the direct-client alternative, kept in its own package on purpose.

It uses MEVD's *dynamic* mapping (`Dictionary<string, object?>`, not a
compile-time-known record type) - the one mapping style needing no
attributes on the record class and no generic type parameter per record
type, matching the reflection-only relationship every other part of XFTY
has with the record types it generates.

## Known and accepted assumptions, and why they're accepted

- **No id or vector-field type is pre-validated.** Unlike
  `Xfty.VectorDatabases.Qdrant`'s gateway, this class does not require a
  `Guid` id or check anything about field shapes before calling MEVD -
  doing so would bake one provider's rule (Qdrant's `Guid`-or-`ulong`
  requirement, say) into code that's supposed to work with providers that
  have entirely different rules. Whatever the concrete `VectorStore`
  rejects, it rejects with its own error, not this package's.
- **Best-effort id auto-fill covers exactly `Guid` and `string`.** If the id
  field is null, this gateway fills it with a new `Guid` (or a
  `Guid`-derived string) based on the field's declared type - the two
  shapes common enough across providers to guess safely. Anything else
  (`int`, `ulong`, a custom key type) is left null, and whatever the
  concrete store does with a null key is between you and that store; a
  `NotSupportedException` is thrown instead only when the id type is
  neither.
- **Exactly one `float[]` property is treated as the vector field**, found
  by reflection (the first property of that exact type) - same convention
  `Xfty.VectorDatabases.Qdrant` uses, for the same reason (no attribute or
  config surface yet).
- **Every record of the same CLR type in one `Insert` call is assumed to
  share one vector dimensionality**, read from the *first* record in the
  group.
- **One collection per CLR type name** (`recordType.Name`, unqualified).
- **Insert-only.** Matches `IPersistenceGateway`'s own single-method
  contract - no read, search, or delete surface.
- **Tested against exactly one connector (Qdrant), not the several this
  package claims to support.** `Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test`
  proves this gateway end-to-end against a real `QdrantVectorStore` -
  Qdrant appears only in the *test* project, purely as one concrete
  example, never as a dependency of the shipped package itself. "Works
  against any MEVD connector" is a design claim borne out by the
  abstraction's own API surface (`GetDynamicCollection` etc. are on the
  base `VectorStore` class, not Qdrant-specific), not something verified
  against Redis, Azure AI Search, or pgvector's own MEVD connector -
  they may well have their own undiscovered quirks the way Qdrant did.
- **Only covered by CI when a Docker-capable runner is present**
  (`[Trait("Category", "Docker")]`) - skips rather than fails without
  Docker.

## Why "preview" and not "beta"

One gateway class, tested against exactly one of the several connectors it
claims to support, built to answer a real question
(see [roadmap/vector-databases.md](../docs/roadmap/vector-databases.md))
rather than as a fully-considered release. A `0.x-preview` version says that
plainly.

## Not built yet

- Verify against a second and third connector (Redis, pgvector's own MEVD
  connector, or Azure AI Search) - the strongest possible evidence the
  "works with any provider" claim actually holds, beyond what the
  abstraction's own type signatures promise.
- Discover the vector field by an explicit convention instead of "the
  first `float[]` property found by reflection."
- Validate that every record in a batch shares one vector dimensionality
  up front, with a clear XFTY-side error.
- Widen id auto-fill past `Guid`/`string`, or accept that as permanent.

See also: [docs/roadmap/vector-databases.md](../docs/roadmap/vector-databases.md).
