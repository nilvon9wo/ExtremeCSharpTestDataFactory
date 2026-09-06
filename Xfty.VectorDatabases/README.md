# Xfty.VectorDatabases

A bundled [`Xfty`](https://www.nuget.org/packages/Xfty) `IValueExpression`
for a vector-database record's embedding field - a fixed-length `float[]` of
independent random values, structurally a vector but **not** a semantically
meaningful embedding (see [Deliberately out of scope](#deliberately-out-of-scope)
below).

```bash
dotnet add package Xfty.VectorDatabases
```

## Usage

```csharp
using Net.Nowhereatall.Xfty.VectorDatabases;

// DocumentChunk here is illustrative - any record with a float[] field works.
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

- **pgvector**, through the unmodified [`Xfty.EntityFrameworkCore`](https://www.nuget.org/packages/Xfty.EntityFrameworkCore)
  gateway - proven against a real Postgres container, zero new gateway code needed.
- **Qdrant**, direct client or via `Microsoft.Extensions.VectorData` - see
  [`Xfty.VectorDatabases.Qdrant`](https://www.nuget.org/packages/Xfty.VectorDatabases.Qdrant) /
  [`Xfty.VectorDatabases.MicrosoftExtensionsVectorData`](https://www.nuget.org/packages/Xfty.VectorDatabases.MicrosoftExtensionsVectorData) -
  both **preview** packages, read their own READMEs first.

## Deliberately out of scope

Calling a real embedding API (OpenAI, Cohere, …) for a semantically
meaningful vector is not a goal of this package - it would break every
`Xfty` value expression's offline-and-fast contract (network latency, API
cost, a credential CI would have to hold). A test asserting a genuine
nearest-neighbor relationship needs vectors informed by its own domain and
should write its own expression for that.

## Full documentation

- [Vector database support (design)](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/roadmap/vector-databases.md) - the full rationale, including why calling a real embedding API is a non-goal
- [Everything else `Xfty` does](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory#readme)
