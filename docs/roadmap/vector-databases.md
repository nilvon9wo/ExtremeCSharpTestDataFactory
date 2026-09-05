# Design: Vector Database Support

Status: ✅ **the value-expression convenience is built** - `Xfty.VectorDatabases`
ships `RandomVectorExpression` plus dimension presets and normalization.
✅ **the pgvector option is proven**, through the existing, unmodified
`EfPersistenceGateway` - see [Persistence](#persistence).
🧪 **two competing dedicated Qdrant gateways exist as a preview
proof-of-concept** - `Xfty.VectorDatabases.Qdrant` (`0.1.0-preview.1`) ships
both a Microsoft.Extensions.VectorData-backed gateway and a raw-`Qdrant.Client`
one, deliberately, to compare them. Both work against a real container, but
neither is the considered, general-purpose package the rest of this
solution's packages are, and the package is expected to split in two if
either graduates - see its own README before using it.
Calling a real embedding API for a semantically meaningful vector is
🚫 **a deliberate non-goal**, not a gap - see the section below.

---

## The shape of the problem

A vector-database record is, structurally, an ordinary record with one
vector-shaped field (`float[]`, `ReadOnlyMemory<float>`, or a driver-specific
type wrapping one) plus whatever metadata accompanies it - and, commonly, a
reference field pointing back at the source content the vector was computed
from. XFTY already generates arbitrary POCOs with arbitrary field types via
reflection, and a reference field is exactly the relationship shape
[`DefaultRelationship`](../use/relationships.md) already models. Nothing
about "the field happens to hold a vector" requires the engine to know or
care.

## What's missing is convenience, not capability

- **A bundled way to produce a plausible vector.** Today, a Provider would
  supply its own `IValueExpression` returning a `float[]` - straightforward,
  but every consumer writing a similar "N random floats" expression from
  scratch is exactly the kind of repeated boilerplate XFTY's bundled value
  expressions (`IncrementingStringExpression`, `UniqueEmailExpression`, …)
  exist to avoid. ✅ Built as `RandomVectorExpression(int dimensions, float
  min = -1f, float max = 1f, bool normalize = false)` in
  `Xfty.VectorDatabases` - a separate package, not core `Xfty`, so the base
  library never depends on any decision about what "a plausible vector"
  means for a use case it can't see. `normalize: true` produces a
  unit-length vector, since cosine-similarity comparisons and several
  vector-DB schemas assume one. `KnownEmbeddingDimensions` bundles named
  dimension constants for popular embedding models
  (`OpenAiTextEmbedding3Small`, `CohereEmbedV3`, …) so a test doesn't have
  to hardcode or look up the number.
- **A more realistic vector for similarity-search tests specifically.** A
  test asserting "the nearest neighbor to X is Y" needs vectors with an
  actual semantic relationship to each other, not independent random noise -
  that's a harder, genuinely domain-specific generation problem no generic
  library can solve without knowing what the vectors represent. Out of scope
  for a value-expression convenience; a test with that need writes its own
  expression informed by its own domain.

## Persistence

[`IPersistenceGateway`](deferred-persistence.md) is still the right seam.
Both options below are now proven against a real container, not just
described - `PgVectorPersistenceTest`, `QdrantPersistenceGatewayTest`, and
`QdrantDirectPersistenceGatewayTest` all actually run and pass, and every
one of them was fixed at least once by a real compiler or runtime error,
not by getting the design right on paper first.

### pgvector, through the existing `EfPersistenceGateway` - proven, no new gateway code

`Pgvector.EntityFrameworkCore` maps a `Vector` column onto an ordinary EF
Core entity property (`[Column(TypeName = "vector(8)")]` plus `UseVector()`
on the Npgsql options). `Xfty.EntityFrameworkCore.Test`'s
`PgVectorPersistenceTest` proves a vector field round-trips through real
persistence with **zero changes to `EfPersistenceGateway` itself** - just
the package reference, a demo entity (`DocumentEmbedding`), and a small
`RandomPgVectorExpression` adapter converting `RandomVectorExpression`'s
`float[]` to `Pgvector.Vector`. One real gotcha: it needs the
`pgvector/pgvector:pg16` image, not the plain `postgres:16-alpine` image
the rest of this project uses - the vector extension has to be compiled
into the Postgres image to be creatable at all. It doesn't exercise a
purpose-built vector database's own indexing/query model the way Qdrant
would, but it's a genuinely near-free way to get a vector column under
real, tested persistence.

### Qdrant - built as two competing preview proofs-of-concept, not a considered package

`Xfty.VectorDatabases.Qdrant` (`0.1.0-preview.1` - see
[its own README](../../Xfty.VectorDatabases.Qdrant/README.md) for the full
list of known assumptions and accepted risks) ships **two** independent
`IPersistenceGateway`s answering the same question two different ways:

- **`QdrantPersistenceGateway`**, through
  `Microsoft.Extensions.VectorData.Abstractions` (GA, 10.9.0) and
  `Microsoft.SemanticKernel.Connectors.Qdrant` (still preview, 1.74.0-preview).
- **`QdrantDirectPersistenceGateway`**, through Qdrant's own client
  (`Qdrant.Client`, stable, 1.19.0) directly - no MEVD, no Semantic Kernel
  connector at all.

Both genuinely work - `QdrantPersistenceGatewayTest` and
`QdrantDirectPersistenceGatewayTest` each insert a real record into a real
Qdrant container. Getting the MEVD one right took two real, concrete
corrections that documentation alone didn't predict:

- **Qdrant's client rejects `string` keys outright** - only `ulong` or
  `Guid` are accepted as a point id. XFTY's own demo record types all use a
  `string` id; both gateways require `Guid` instead and throw a clear error
  otherwise. Checked on both paths, not assumed from one: the MEVD
  connector surfaces this as a *runtime* validation error, while the raw
  client's `PointId` type has no `string` conversion at all, failing at
  **compile time** instead - two different failure modes confirming the
  same real Qdrant-level constraint, not an artifact of either specific
  library.
- **A vector property's declared schema `Type` must be the actual container
  type** (`float[]`), not the element type (`float`) - Microsoft's own
  published example for a different provider used the element type, which
  fails against Qdrant specifically. This one is MEVD-only; the direct
  client has no declared-`Type` schema step for it to apply to at all
  (`PointStruct.Vectors` just takes a `float[]`).

**The comparison this set out to answer:** the direct gateway compiled and
passed on the first real attempt (reusing the already-known id constraint,
independently reconfirmed rather than assumed); the MEVD gateway needed the
second correction above, unique to its abstraction layer. One data point,
not a verdict - MEVD's real win is schema/mapping code you don't hand-write
once a record looks like Microsoft's own `Hotel`-shaped examples (several
data properties, multiple vector fields); for the simple shape this PoC
tested, going without it was both simpler and had less documentation-drift
risk. Both were only found reliable by actually running them against a live
container, which is exactly why this whole package is versioned `preview`,
not `beta`.

**If either graduates past a proof-of-concept, they should split into
separate packages** - not stay bundled the way this comparison phase has
them. A consumer who only wants the direct gateway shouldn't be forced to
take a transitive dependency on Microsoft's still-preview, Semantic-Kernel-
branded connector for a class that never uses it, and the two gateways'
dependencies will graduate out of preview on different vendors' schedules -
see the package README's own section on this for the full reasoning.

Qdrant remains the right first target over Pinecone or Azure AI Search if a
considered gateway is built later - it has an official Docker image and a
`Testcontainers.Qdrant` module (same major version already pinned for
Postgres in this repo), fitting the existing no-cloud-credentials-in-CI
pattern. Pinecone is cloud-managed only, with no equivalent local/Docker
story.

## Deliberately out of scope: calling a real embedding model

Everything above assumes the vector is either random or supplied by the
Provider. A different idea - an `Xfty.Embeddings`-style package that calls a
real embedding API (OpenAI, Azure OpenAI, Cohere, …) to produce a
semantically meaningful vector for a test - is not on this roadmap, and not
just because nobody's asked:

- **It breaks XFTY's offline-and-fast contract.** Every value expression in
  XFTY, `Xfty.Bogus` included, runs with no network call and no external
  dependency beyond an in-process library. Calling a real embedding
  endpoint means network latency, API cost, and a credential a CI pipeline
  would have to hold - none of which is true of anything else XFTY
  generates.
- **It's a different tool with a different risk profile**, not a bigger
  version of what value expressions already do. A project that genuinely
  needs real embeddings for a similarity-search test is better served by a
  small helper *in that project*, calling the embedding API it already
  uses in production - not by XFTY adopting a pattern (paid, non-local API
  calls at test-setup time) it doesn't use anywhere else.

If this changes - if enough real projects need it that a shared, opt-in
answer is worth having - it would need its own explicit design discussion
about caching, cost, and how it stays optional. It is not a natural
extension of `RandomVectorExpression`; it is a different category of thing.

## Conclusion

The convenience value expression is built, including the model-dimension
and normalization refinements. pgvector persistence is proven through the
existing `EfPersistenceGateway` with no new gateway code. Two competing
dedicated Qdrant gateways exist and both work, as preview proofs-of-concept
comparing Microsoft.Extensions.VectorData against Qdrant's own client
directly - real, moderate work, confirmed (not assumed) findings on both
sides, not yet a considered package, and expected to split into separate
packages if either one graduates. Calling a real embedding API is a
deliberate non-goal, not a gap.

See also: [roadmap/README.md](README.md).
