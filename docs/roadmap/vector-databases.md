# Design: Vector Database Support

Status: ✅ **built.** `Xfty.VectorDatabases` ships `RandomVectorExpression` -
the one convenience this page identified as missing. Everything below is
kept as the reasoning for why that was the right, and the only, thing to
build here.

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
  min = -1f, float max = 1f)` in `Xfty.VectorDatabases` - a separate package,
  not core `Xfty`, so the base library never depends on any decision about
  what "a plausible vector" means for a use case it can't see.
- **A more realistic vector for similarity-search tests specifically.** A
  test asserting "the nearest neighbor to X is Y" needs vectors with an
  actual semantic relationship to each other, not independent random noise -
  that's a harder, genuinely domain-specific generation problem no generic
  library can solve without knowing what the vectors represent. Out of scope
  for a value-expression convenience; a test with that need writes its own
  expression informed by its own domain.

## Persistence

Exactly [`IPersistenceGateway`](deferred-persistence.md) already covers this:
a `PineconePersistenceGateway`, `QdrantPersistenceGateway`, or similar
implementing `Insert(records, idField)` against the relevant SDK's upsert
call is all it would take, following the same pattern
`EfPersistenceGateway` already establishes for EF Core. Nothing in `Xfty`
core would need to change.

## Conclusion

The only real gap was the convenience value expression, and it's built. A
`PineconePersistenceGateway`/`QdrantPersistenceGateway` remains unbuilt -
nobody has needed one yet - but the seam for it already exists and needs no
design work when someone does.

See also: [roadmap/README.md](README.md).
