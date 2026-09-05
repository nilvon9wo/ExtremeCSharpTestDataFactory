# Xfty.VectorDatabases.Qdrant

**Status: PREVIEW / proof-of-concept.** Versioned `0.1.0-preview.1`, not
`1.0.0-beta.1` like every other package in this repo, on purpose - see
[Why "preview" and not "beta"](#why-preview-and-not-beta) below. It works
(verified against a real Qdrant container, not asserted), but it exists to
answer a research question - "how big a lift is a real vector-database
`IPersistenceGateway`?" - not as a fully-considered, general-purpose package
yet. Read this whole file before using it for anything beyond that question.

## What it does

`QdrantPersistenceGateway` implements XFTY's `IPersistenceGateway` -
inserting `RecordProvider`-generated records into a real Qdrant collection
instead of mocking their Ids. It uses
[`Microsoft.Extensions.VectorData`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.vectordata)'s
*dynamic* mapping (`Dictionary<string, object?>`, not a compile-time-known
record type), because that's the one mapping style that needs no attributes
on the record class and no generic type parameter per record type - the
same reflection-only relationship every other part of XFTY has with the
record types it generates.

## Known and accepted assumptions, and why they're accepted

- **The id field must be `Guid` (or `Guid?`).** Discovered by running this
  code against a real container, not assumed up front: Qdrant's connector
  rejects `string` keys outright (`ulong` also works, but this gateway
  doesn't support it - see [Not built yet](#not-built-yet)). If your record
  type's id is a `string` (XFTY's own bundled demo types all use one),
  `QdrantPersistenceGateway` throws a clear `NotSupportedException` at
  insert time rather than letting Qdrant's own opaque validation error
  surface.
- **Sync-over-async bridging.** Every real operation
  (`EnsureCollectionExistsAsync`, `UpsertAsync`) is `Task`-based; XFTY's
  `IPersistenceGateway.Insert` is `void`. This gateway bridges with
  `.GetAwaiter().GetResult()`. Accepted for test-setup code running in a
  console-style host with no captured `SynchronizationContext` (xUnit, CI) -
  it would be a real deadlock risk if this gateway were reused inside a
  classic ASP.NET request or a WinForms/WPF UI thread. Don't do that.
- **Exactly one `float[]` property is treated as the vector field**, found
  by reflection (the first property of that exact type). A record with two
  `float[]` properties has its second one silently treated as an ordinary
  data field; a record with none throws `InvalidOperationException` from
  `.First()`.
- **Every record of the same CLR type in one `Insert` call is assumed to
  share one vector dimensionality**, read from the *first* record in the
  group. A batch mixing differently-sized vectors for the same type produces
  a Qdrant-side schema error, not a clear one from this gateway.
- **One Qdrant collection per CLR type name** (`recordType.Name`, unqualified
  - no namespace). Two distinct record types that happen to share a short
  name would collide into the same collection.
- **No per-property configuration** - every data property is declared plain,
  every vector property uses Qdrant/`Microsoft.Extensions.VectorData`'s
  default distance function and index kind. No choice of `DistanceFunction`
  or `IndexKind` is exposed.
- **Insert-only.** Matches `IPersistenceGateway`'s own single-method
  contract - this package has no read, search, or delete surface. Querying
  what you inserted is entirely outside XFTY's scope, same as every other
  gateway.
- **Built against still-preview upstream packages.** `Microsoft.Extensions.VectorData.Abstractions`
  is GA (10.9.0), but `Microsoft.SemanticKernel.Connectors.Qdrant`
  (1.74.0-preview, pinned here) is not, and its API has changed shape as
  recently as May 2025 per Microsoft's own migration notes. Two lines in
  this gateway were wrong on the first attempt, based on what looked like
  authoritative documentation, and were only caught by actually building
  and running this test against a live container - not by trusting the
  docs. A future version bump of that dependency could change behavior or
  break the build without warning; this package's version is pinned
  deliberately, and bumping it should mean re-running the real test, not
  just restoring.
- **Only covered by CI when a Docker-capable runner is present**
  (`[Trait("Category", "Docker")]`, same opt-in pattern as the Postgres tier
  in `Xfty.EntityFrameworkCore.Test`) - it skips rather than fails without
  Docker.

## Why "preview" and not "beta"

Every other package in this solution (`Xfty`, `Xfty.EntityFrameworkCore`,
`Xfty.Bogus`, `Xfty.VectorDatabases`) is `1.0.0-beta.1` - a considered,
reasonably general-purpose surface with real test coverage. This package is
one gateway class, one demo type, one test, built specifically to answer
"is a real vector-DB gateway a trivial wrapper or real design work" (real
design work - see [roadmap/vector-databases.md](../docs/roadmap/vector-databases.md)).
It depends on another vendor's preview package, several of the assumptions
above are real limitations rather than deliberate design choices, and it
has not been used against an actual project's schema. `0.1.0-preview.1`
says exactly that.

## Not built yet

If this graduates past a proof-of-concept:

- Support `ulong` keys, not just `Guid`.
- Discover the vector field by an explicit convention (an attribute, or a
  config lambda) instead of "the first `float[]` property found by
  reflection."
- Validate that every record in a batch shares one vector dimensionality
  up front, with a clear XFTY-side error instead of an opaque one from
  Qdrant.
- Expose `DistanceFunction`/`IndexKind` configuration instead of always
  taking the provider's default.
- Track `Microsoft.SemanticKernel.Connectors.Qdrant` to a non-preview
  release once one exists, and re-verify this gateway against it.

See also: [docs/roadmap/vector-databases.md](../docs/roadmap/vector-databases.md).
