# Design: Vector Database Support

Status: **idea, likely needs little to nothing new.** Flagged here so it's
tracked, not because a gap has actually been found.

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
  exist to avoid. A `RandomVectorExpression(int dimensions)` would be a small,
  self-contained addition whenever someone actually wants one.
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

Unless a real gap surfaces in practice, this isn't a design problem so much
as an "add a convenience value expression when someone wants one" item. Kept
here so it isn't forgotten, and so a future gap - if one turns up - has a
place to be recorded against.

See also: [roadmap/README.md](README.md).
