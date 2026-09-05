# Design: Vector Database Support

Status: ✅ **the value-expression convenience is built** - `Xfty.VectorDatabases`
ships `RandomVectorExpression` plus dimension presets and normalization. A
dedicated vector-database persistence gateway (Qdrant or otherwise) is
📋 **designed but not built** - real, moderate work, tracked below rather
than done speculatively. Calling a real embedding API for a semantically
meaningful vector is 🚫 **a deliberate non-goal**, not a gap - see the
section below.

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

[`IPersistenceGateway`](deferred-persistence.md) is still the right seam,
but a real gateway for a dedicated vector database is a genuinely bigger
task than `EfPersistenceGateway` was, for two concrete reasons researched
against the current .NET ecosystem (as of September 2026), not assumed:

- **Every real vector-DB client is async-only.** Qdrant's official client
  (`Qdrant.Client`, gRPC-backed) exposes only `UpsertAsync`; there is no
  synchronous variant to call the way `EfPersistenceGateway` calls EF Core's
  synchronous `SaveChanges()`. A gateway would need to bridge sync-over-async
  (`.GetAwaiter().GetResult()`) - an accepted pattern for test-setup code,
  but a real design choice, not a detail to skip past.
- **The emerging .NET standard doesn't map on as cleanly as EF Core did.**
  `Microsoft.Extensions.VectorData.Abstractions` (GA, currently 10.9.0) is a
  real, Microsoft-backed common abstraction over vector stores - the closest
  thing to "EF Core for vector databases" that exists today, with connectors
  for Qdrant, Azure AI Search, PostgreSQL/pgvector, Redis, and others. But
  its collection API (`GetCollection<TKey, TRecord>(name)`) is generic per
  record type and async-only, unlike EF Core's untyped, synchronous
  `DbContext.Add(object)` / `SaveChanges()` - and most individual connectors
  (including Qdrant's) are still preview-labeled by Microsoft even though
  the abstraction layer itself is GA. A `VectorStoreRecordDefinition` built
  at runtime (rather than attributes on the POCO) would keep a gateway
  reflection-based and attribute-free, consistent with how XFTY treats every
  other record type - real, moderate work, not a trivial wrapper.

**A cheaper first step than a dedicated vector-DB gateway: pgvector,
through the *existing* `EfPersistenceGateway`.** `Pgvector.EntityFrameworkCore`
maps a `Vector` column onto an ordinary EF Core entity property
(`[Column(TypeName = "vector(1536)")]` plus `UseVector()` on the Npgsql
options). Since `Xfty.EntityFrameworkCore.Test` already runs a real Postgres
container via Testcontainers, proving a vector field round-trips through
real persistence would need *no new gateway code at all* - just a package
reference and a demo entity shape. It doesn't exercise a purpose-built
vector database's own indexing/query model the way Qdrant would, but it's a
near-free way to validate the concept before committing to a dedicated
gateway.

If a dedicated vector-DB gateway is ever built, Qdrant is the right first
target over Pinecone or Azure AI Search: it has an official Docker image and
a `Testcontainers.Qdrant` module (same major version already pinned for
Postgres in this repo), so it fits the existing no-cloud-credentials-in-CI
pattern. Pinecone is cloud-managed only, with no equivalent local/Docker
story - a real barrier to testing it the way every other persistence tier in
this repo is tested.

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
and normalization refinements. Real persistence for a dedicated vector
database remains unbuilt and is real, moderate work when someone needs it,
not a trivial wrapper - pgvector via the existing `EfPersistenceGateway` is
the cheaper way to validate the concept first. Calling a real embedding API
is a deliberate non-goal, not a gap.

See also: [roadmap/README.md](README.md).
