# Xfty.VectorDatabases.Qdrant

[![NuGet](https://img.shields.io/nuget/v/Xfty.VectorDatabases.Qdrant.svg)](https://www.nuget.org/packages/Xfty.VectorDatabases.Qdrant/)

**Status: PREVIEW / proof-of-concept.** Versioned `0.x-preview`, not
`1.0.0-beta.1` like every other package in this repo, on purpose - see
[Why "preview" and not "beta"](#why-preview-and-not-beta) below. It works
(verified against a real Qdrant container, not asserted), but it exists to
answer a research question - "is talking to Qdrant directly simpler and
safer than going through Microsoft.Extensions.VectorData?" - not as a
fully-considered, general-purpose package yet. Read this whole file before
using it for anything beyond that question.

```bash
dotnet add package Xfty.VectorDatabases.Qdrant
```

## What it does

`QdrantPersistenceGateway` implements XFTY's `IPersistenceGateway`,
inserting `RecordProvider`-generated records into a real Qdrant collection
through `Qdrant.Client` directly - no Microsoft.Extensions.VectorData
(MEVD), no Semantic Kernel connector at all. It builds `PointStruct`s and a
`Dictionary`-style payload by hand via reflection
(`QdrantRecordReflection`), rather than getting that mapping from a shared
abstraction. Depends only on Qdrant's own stable client (`Qdrant.Client`,
1.19.0) - nothing preview, nothing Semantic-Kernel-branded.

There's also a separate package,
[`Xfty.VectorDatabases.MicrosoftExtensionsVectorData`](../Xfty.VectorDatabases.MicrosoftExtensionsVectorData/README.md),
doing the same job through Microsoft's `VectorStore` abstraction instead of
Qdrant's client directly - its own package on purpose, so depending on this
one never pulls in MEVD or a Semantic-Kernel-branded connector this class
doesn't use. See [roadmap/vector-databases.md](../docs/roadmap/vector-databases.md#why-two-packages-not-one)
for the full comparison between the two approaches, including the one real,
concrete difference the comparison turned up (a schema-`Type` gotcha unique
to the MEVD path).

## Known and accepted assumptions, and why they're accepted

- **The id field must be `Guid` (or `Guid?`).** Qdrant's client has no
  string overload for a point id at all - `PointId` only accepts `Guid` or
  `ulong` - so this is a real constraint of Qdrant itself, not an artifact
  of any particular library sitting in front of it. `QdrantRecordReflection.RequireGuidKey`
  throws a clear `NotSupportedException` at insert time rather than letting
  a `CS0029`-style failure or Qdrant's own opaque error surface instead.
- **Exactly one `float[]` property is treated as the vector field**, found
  by reflection (the first property of that exact type). A record with two
  `float[]` properties has its second one silently treated as an ordinary
  payload field; a record with none throws `InvalidOperationException` from
  `.First()`.
- **Every record of the same CLR type in one `Insert` call is assumed to
  share one vector dimensionality**, read from the *first* record in the
  group.
- **One Qdrant collection per CLR type name** (`recordType.Name`,
  unqualified - no namespace). Two distinct record types that happen to
  share a short name would collide into the same collection.
- **Hardcodes `Distance.Cosine`** for every collection it creates - no way
  to choose a different distance function or index kind.
- **Payload mapping only supports `string`/`bool`/`int`/`long`/`float`/`double`**
  data properties (a plain `switch` on the CLR value) - anything else
  throws a clear `NotSupportedException` rather than failing inside
  Qdrant's client with a less obvious error.
- **Insert-only.** Matches `IPersistenceGateway`'s own single-method
  contract - no read, search, or delete surface.
- **Only covered by CI when a Docker-capable runner is present**
  (`[Trait("Category", "Docker")]`, same opt-in pattern as the Postgres tier
  in `Xfty.EntityFrameworkCore.Test`) - it skips rather than fails without
  Docker.

## Why "preview" and not "beta"

Every other package in this solution (`Xfty`, `Xfty.EntityFrameworkCore`,
`Xfty.Bogus`, `Xfty.VectorDatabases`) is `1.0.0-beta.1` - a considered,
reasonably general-purpose surface with real test coverage. This package is
one gateway class, one demo type, one test, built specifically to answer a
comparison question - see [roadmap/vector-databases.md](../docs/roadmap/vector-databases.md).
Several of the assumptions above are real limitations rather than
deliberate design choices, and it has not been used against an actual
project's schema. A `0.x-preview` version says exactly that.

## Not built yet

- Support `ulong` keys, not just `Guid`.
- Discover the vector field by an explicit convention (an attribute, or a
  config lambda) instead of "the first `float[]` property found by
  reflection."
- Validate that every record in a batch shares one vector dimensionality
  up front, with a clear XFTY-side error instead of an opaque one from
  Qdrant.
- Expose `Distance`/index-kind configuration instead of the hardcoded
  default.
- Widen the payload mapping past `string`/`bool`/`int`/`long`/`float`/`double`,
  or decide that's a fine permanent limitation.

See also: [docs/roadmap/vector-databases.md](../docs/roadmap/vector-databases.md),
[Xfty.VectorDatabases.MicrosoftExtensionsVectorData](../Xfty.VectorDatabases.MicrosoftExtensionsVectorData/README.md).
