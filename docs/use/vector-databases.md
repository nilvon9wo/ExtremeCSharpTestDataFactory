# Vector Database Fields

`Xfty.VectorDatabases` is a separate, opt-in package: one bundled
`IValueExpression` for a vector-database record's embedding field - a
fixed-length `float[]` of independent random values, structurally a vector
but **not** a semantically meaningful embedding (see
[Deliberately out of scope](#deliberately-out-of-scope) below).

```bash
dotnet add package Xfty.VectorDatabases
```

## Usage

```csharp
using Net.Nowhereatall.Xfty.VectorDatabases;

new MasterTemplate<DocumentChunk>(x => x.Id)
{
    [x => x.Embedding] = new RandomVectorExpression(KnownEmbeddingDimensions.OpenAiTextEmbedding3Small),
};
```

`RandomVectorExpression(int dimensions, float min = -1f, float max = 1f, bool normalize = false)` -
`normalize: true` produces a unit-length vector, since cosine-similarity
comparisons and several vector-DB schemas assume one.
`KnownEmbeddingDimensions` bundles named dimension constants for popular
embedding models (`OpenAiTextEmbedding3Small`, `CohereEmbedV3`, …) so a test
doesn't have to hardcode or look up the number.

## Persisting a vector field

This package only produces the value. For actual persistence:

- **pgvector**, through the unmodified `Xfty.EntityFrameworkCore` gateway
  (see [insert-modes](insert-modes.md#now)) - proven against a real Postgres
  container, zero new gateway code needed.
- **Qdrant**, direct client or via `Microsoft.Extensions.VectorData` - see
  `Xfty.VectorDatabases.Qdrant` / `Xfty.VectorDatabases.MicrosoftExtensionsVectorData` -
  both **preview** packages, read each one's own README first.

## Deliberately out of scope

Calling a real embedding API (OpenAI, Cohere, …) for a semantically
meaningful vector is not a goal of this package - it would break every
`Xfty` value expression's offline-and-fast contract (network latency, API
cost, a credential CI would have to hold). A test asserting a genuine
nearest-neighbor relationship needs vectors informed by its own domain and
should write its own expression for that.

See also: [roadmap/vector-databases.md](../roadmap/vector-databases.md) - the
full design rationale, including the two preview persistence packages.

Runnable: `VectorDatabasesReadmeExampleTest`
