# Xfty.VectorDatabases.Qdrant

**Status: PREVIEW / proof-of-concept.** Versioned `0.1.0-preview.1`, not
`1.0.0-beta.1` like every other package in this repo, on purpose - see
[Why "preview" and not "beta"](#why-preview-and-not-beta) below. It works
(verified against a real Qdrant container, not asserted), but it exists to
answer a research question - "is talking to Qdrant directly simpler and
safer than going through Microsoft.Extensions.VectorData?" - not as a
fully-considered, general-purpose package yet. Read this whole file before
using it for anything beyond that question.

## What it does

`QdrantPersistenceGateway` implements XFTY's `IPersistenceGateway`,
inserting `RecordProvider`-generated records into a real Qdrant collection
through `Qdrant.Client` directly - no Microsoft.Extensions.VectorData
(MEVD), no Semantic Kernel connector at all. It builds `PointStruct`s and a
`Dictionary`-style payload by hand via reflection
(`QdrantRecordReflection`), rather than getting that mapping from a shared
abstraction. Depends only on Qdrant's own stable client (`Qdrant.Client`,
1.19.0) - nothing preview, nothing Semantic-Kernel-branded.

**This package used to also ship an MEVD-backed gateway, for comparison.**
It was split out on purpose, not left combined even during preview: keeping
them together meant anyone who wanted only the direct-client approach was
already forced to take a transitive dependency on MEVD and Microsoft's
still-preview Qdrant connector, starting from the moment they installed
this package - not a future cost, a real one from day one. The MEVD-backed
gateway now lives in its own package,
[`Xfty.VectorDatabases.MicrosoftExtensionsVectorData`](../Xfty.VectorDatabases.MicrosoftExtensionsVectorData/README.md)
- and turned out to deserve its own package for a second reason: it isn't
actually Qdrant-specific at all (see that package's README).

**The comparison that motivated this split:** the direct gateway here
compiled and passed against a real container on the first real attempt.
The MEVD-backed gateway needed a correction unique to its abstraction layer
(a vector property's schema `Type` has to be the container type, not the
element type - no equivalent step exists on this direct path at all, since
`PointStruct.Vectors` just takes a `float[]`). One shared constraint - the
`Guid`-only id, below - was independently reconfirmed on this path too, not
assumed to carry over: assigning a plain `string` to `PointStruct.Id` fails
at **compile time** here (`CS0029`), versus a *runtime* validation error on
the MEVD path - two different failure modes, the same real Qdrant
constraint either way. MEVD's actual win is schema/mapping code you don't
write by hand, useful once records look like Microsoft's own `Hotel`-style
examples (several data properties, multiple vector fields); for the simple
shape this PoC tested, going without it was both simpler and had less
documentation-drift risk.

## Known and accepted assumptions, and why they're accepted

- **The id field must be `Guid` (or `Guid?`).** Qdrant's client has no
  string overload for a point id at all - `PointId` only accepts `Guid` or
  `ulong` - so this is a real constraint of Qdrant itself, not an artifact
  of any particular library sitting in front of it. `QdrantRecordReflection.RequireGuidKey`
  throws a clear `NotSupportedException` at insert time rather than letting
  a `CS0029`-style failure or Qdrant's own opaque error surface instead.
- **Sync-over-async bridging.** Every real operation
  (`CollectionExistsAsync`, `CreateCollectionAsync`, `UpsertAsync`) is
  `Task`-based; XFTY's `IPersistenceGateway.Insert` is `void`. This gateway
  bridges with `.GetAwaiter().GetResult()`. Accepted for test-setup code
  running in a console-style host with no captured `SynchronizationContext`
  (xUnit, CI) - it would be a real deadlock risk if this gateway were
  reused inside a classic ASP.NET request or a WinForms/WPF UI thread.
  Don't do that.
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
project's schema. `0.1.0-preview.1` says exactly that.

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
