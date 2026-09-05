# Design: Vector Database Support

Status: ✅ **the value-expression convenience is built** - `Xfty.VectorDatabases`
ships `RandomVectorExpression` plus dimension presets and normalization.
✅ **the pgvector option is proven**, through the existing, unmodified
`EfPersistenceGateway` - see [Persistence](#persistence).
🧪 **a dedicated Qdrant gateway exists as a preview proof-of-concept** -
`Xfty.VectorDatabases.Qdrant`, versioned `0.1.0-preview.1`, works against a
real container, but is not the considered, general-purpose package the rest
of this solution's packages are - see its own README before using it.
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
described - `PgVectorPersistenceTest` and `QdrantPersistenceGatewayTest`
both actually run and pass, and both were fixed at least once by real
compiler/runtime errors, not by getting the design right on paper first.

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

### Qdrant - built as a preview proof-of-concept, not a considered package

`Xfty.VectorDatabases.Qdrant` (`0.1.0-preview.1` - see
[its own README](../../Xfty.VectorDatabases.Qdrant/README.md) for the full
list of known assumptions and accepted risks) ships
`QdrantPersistenceGateway`, built against
`Microsoft.Extensions.VectorData.Abstractions` (GA, 10.9.0) and
`Microsoft.SemanticKernel.Connectors.Qdrant` (still preview, 1.74.0-preview).
It genuinely works - `QdrantPersistenceGatewayTest` inserts a real record
into a real Qdrant container - but getting there took two real, concrete
corrections that documentation alone didn't predict:

- **Qdrant's connector rejects `string` keys outright** - only `ulong` or
  `Guid` are accepted as a key property type. XFTY's own demo record types
  all use a `string` id; this gateway requires `Guid` instead and throws a
  clear error otherwise.
- **A vector property's declared `Type` must be the actual container type**
  (`float[]`), not the element type (`float`) - Microsoft's own published
  example for a different provider used the element type, which fails
  against Qdrant specifically.

Both were only found by actually running the code against a live container,
which is exactly why this package is versioned `preview`, not `beta`: an
API surface with this much documentation drift under it deserves lower
confidence than the rest of this solution, until it's been used against a
real project's own schema. Qdrant remains the right first target over
Pinecone or Azure AI Search if a considered gateway is built later - it has
an official Docker image and a `Testcontainers.Qdrant` module (same major
version already pinned for Postgres in this repo), fitting the existing
no-cloud-credentials-in-CI pattern. Pinecone is cloud-managed only, with no
equivalent local/Docker story.

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
existing `EfPersistenceGateway` with no new gateway code. A dedicated Qdrant
gateway exists and works, but only as a preview proof-of-concept - real,
moderate work confirmed by two concrete corrections the documentation
didn't predict, not a trivial wrapper, and not yet a considered package.
Calling a real embedding API is a deliberate non-goal, not a gap.

See also: [roadmap/README.md](README.md).
