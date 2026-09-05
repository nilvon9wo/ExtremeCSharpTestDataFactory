# Xfty.VectorDatabases.Qdrant

**Status: PREVIEW / proof-of-concept.** Versioned `0.1.0-preview.1`, not
`1.0.0-beta.1` like every other package in this repo, on purpose - see
[Why "preview" and not "beta"](#why-preview-and-not-beta) below. It works
(verified against a real Qdrant container, not asserted), but it exists to
answer a research question - "how big a lift is a real vector-database
`IPersistenceGateway`?" - not as a fully-considered, general-purpose package
yet. Read this whole file before using it for anything beyond that question.

## What it does

This package ships **two** independent `IPersistenceGateway` implementations,
both inserting `RecordProvider`-generated records into a real Qdrant
collection instead of mocking their Ids, so the actual question - is
Microsoft's abstraction worth it, or is talking to Qdrant directly just as
easy - has a real answer instead of a guess:

- **`QdrantPersistenceGateway`** goes through
  [`Microsoft.Extensions.VectorData`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.vectordata)'s
  *dynamic* mapping (`Dictionary<string, object?>`, not a compile-time-known
  record type) - the one MEVD mapping style that needs no attributes on the
  record class and no generic type parameter per record type, matching the
  reflection-only relationship every other part of XFTY has with the record
  types it generates. Depends on `Microsoft.SemanticKernel.Connectors.Qdrant`
  (still preview) as well as `Qdrant.Client`.
- **`QdrantDirectPersistenceGateway`** talks to `Qdrant.Client` directly -
  no MEVD, no Semantic Kernel connector at all - building `PointStruct`s and
  a `Dictionary`-style payload by hand via the same reflection helpers
  (`QdrantRecordReflection`) the MEVD gateway uses for id/vector-field
  discovery. Depends only on `Qdrant.Client` (stable, 1.19.0).

**The comparison result:** the direct gateway compiled and passed against a
real container on the first real attempt, using the id/vector-field
constraints already known from the MEVD gateway - it was not written from
a blank slate, and one of those two known constraints (the `Guid`-only id)
was independently re-confirmed against the raw client rather than assumed
to carry over (see the id bullet below: it turned out to be a real Qdrant
client constraint either way, just caught at compile time instead of
runtime). The other MEVD correction - a vector property's schema `Type`
having to be the container type, not the element type - has no equivalent
concept on the direct path at all, since `PointStruct.Vectors` takes a
`float[]` directly with no declared-`Type` schema step to get wrong. So the
honest comparison is: one shared constraint, confirmed on both paths by two
different failure modes; one MEVD-only pitfall that doing without MEVD
simply has no room for, because it doesn't have the abstraction step that
pitfall lives in. MEVD's actual win is schema/mapping code you don't write
by hand (useful once records get closer to the `Hotel`-style examples in
Microsoft's own docs - a data property mix, multiple vector fields, etc.);
for the simple one-vector-plus-a-few-scalars shape this PoC tested, the
direct client was both simpler to write and had less documentation-drift
risk to get right.

## Known and accepted assumptions, and why they're accepted

Both gateways share these unless noted otherwise:

- **The id field must be `Guid` (or `Guid?`).** Confirmed independently on
  both paths, not assumed from one: `QdrantPersistenceGateway` discovered it
  as a *runtime* validation error from the MEVD/SK connector
  (`QdrantModelBuilder.ValidateKeyProperty`: "Key properties must be either
  ulong or Guid"). Rather than assume `QdrantDirectPersistenceGateway`
  shared that constraint for the same reason, it was checked directly:
  assigning a plain `string` to `PointStruct.Id` fails at **compile time**
  (`CS0029: Cannot implicitly convert type 'string' to 'PointId'`) - Qdrant's
  own client has no string overload for a point id at all, so this is a
  real constraint of Qdrant itself, not an artifact of the MEVD connector's
  own extra validation. Both gateways throw a clear `NotSupportedException`
  at insert time (`QdrantRecordReflection.RequireGuidKey`) well before
  either failure mode would otherwise surface.
- **Sync-over-async bridging.** Every real operation on both the MEVD
  collection (`EnsureCollectionExistsAsync`, `UpsertAsync`) and the raw
  client (`CollectionExistsAsync`, `CreateCollectionAsync`, `UpsertAsync`)
  is `Task`-based; XFTY's `IPersistenceGateway.Insert` is `void`. Both
  gateways bridge with `.GetAwaiter().GetResult()`. Accepted for test-setup
  code running in a console-style host with no captured
  `SynchronizationContext` (xUnit, CI) - it would be a real deadlock risk if
  either gateway were reused inside a classic ASP.NET request or a
  WinForms/WPF UI thread. Don't do that.
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
- **No per-property configuration.** `QdrantPersistenceGateway` leaves every
  vector property at MEVD/Qdrant's default distance function and index
  kind. `QdrantDirectPersistenceGateway` hardcodes `Distance.Cosine` for
  every collection it creates. Neither exposes a way to choose.
  `QdrantDirectPersistenceGateway`'s payload mapping is also narrower: only
  `string`/`bool`/`int`/`long`/`float`/`double` data properties are
  supported (a plain `switch` on the CLR value) - anything else throws a
  clear `NotSupportedException` rather than failing inside Qdrant's client.
- **Insert-only.** Matches `IPersistenceGateway`'s own single-method
  contract - this package has no read, search, or delete surface. Querying
  what you inserted is entirely outside XFTY's scope, same as every other
  gateway.
- **`QdrantPersistenceGateway` is built against a still-preview upstream
  package; `QdrantDirectPersistenceGateway` is not.**
  `Microsoft.Extensions.VectorData.Abstractions` is GA (10.9.0), but
  `Microsoft.SemanticKernel.Connectors.Qdrant` (1.74.0-preview, pinned here)
  is not, and its API has changed shape as recently as May 2025 per
  Microsoft's own migration notes. Two lines in `QdrantPersistenceGateway`
  were wrong on the first attempt, based on what looked like authoritative
  documentation, and were only caught by actually building and running the
  test against a live container - not by trusting the docs.
  `QdrantDirectPersistenceGateway`, depending only on the stable
  `Qdrant.Client` (1.19.0), compiled and passed on the first real attempt -
  see [What it does](#what-it-does) for what that comparison is worth. A
  future version bump of the preview connector could change behavior or
  break the build without warning; this package's versions are pinned
  deliberately, and bumping either should mean re-running the real tests,
  not just restoring.
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

## If either gateway graduates past a proof-of-concept: split this package

They should **not** stay in one package once either is a real, considered
release - this combined layout is deliberately only for the PoC/comparison
phase. Reasons, not just a preference:

- **Different dependency footprints.** A consumer who only wants
  `QdrantDirectPersistenceGateway` would still pull in
  `Microsoft.Extensions.VectorData.Abstractions` and the
  Semantic-Kernel-branded `Microsoft.SemanticKernel.Connectors.Qdrant` as
  transitive dependencies of the same package, for a class that doesn't use
  either. That's exactly the kind of forced, unwanted dependency this
  solution's whole package-per-concern split (`Xfty.Bogus`,
  `Xfty.VectorDatabases`, `Xfty.EntityFrameworkCore` all separate,
  none required by the others) exists to avoid.
- **Independent maturity timelines, controlled by different vendors.**
  `Qdrant.Client` is already stable. `Microsoft.SemanticKernel.Connectors.Qdrant`
  graduates out of preview on Microsoft's own schedule, unrelated to
  Qdrant's. A single package version would either force the direct gateway
  to wait on someone else's preview package, or force a "1.0" release that
  still contains a preview-dependent class - both wrong.
- **The comparison itself stops mattering once one wins.** Once either
  gateway is the one to depend on, keeping the also-ran in the same package
  just adds it to every consumer's dependency tree with no comparison left
  to make.

The split, when it happens: `Xfty.VectorDatabases.Qdrant` keeps
`QdrantDirectPersistenceGateway` (it's the more natural home for the
package name, and depends on nothing but Qdrant's own client); the MEVD
gateway moves to something like `Xfty.VectorDatabases.MicrosoftExtensionsVectorData`
(or is dropped, if the direct gateway turns out to be the one worth
keeping - the comparison above is one data point, not a final verdict).
`QdrantRecordReflection`'s id/vector-field logic is small enough to
duplicate across both rather than introduce a third shared package for it.

## Not built yet

If either gateway graduates past a proof-of-concept:

- **Split the package first** - see above. Everything below applies to
  whichever gateway(s) survive that split.
- Support `ulong` keys, not just `Guid`.
- Discover the vector field by an explicit convention (an attribute, or a
  config lambda) instead of "the first `float[]` property found by
  reflection."
- Validate that every record in a batch shares one vector dimensionality
  up front, with a clear XFTY-side error instead of an opaque one from
  Qdrant.
- Expose `DistanceFunction`/`IndexKind` configuration instead of always
  taking a hardcoded or default one.
- Widen `QdrantDirectPersistenceGateway`'s payload mapping past
  `string`/`bool`/`int`/`long`/`float`/`double`, or decide that's a fine
  permanent limitation.
- If `QdrantPersistenceGateway` survives the split: track
  `Microsoft.SemanticKernel.Connectors.Qdrant` to a non-preview release once
  one exists, and re-verify against it.

See also: [docs/roadmap/vector-databases.md](../docs/roadmap/vector-databases.md).
