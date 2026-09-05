# Design: Embedded/Denormalized Document Relationships

Status: **idea, not designed.** No code exists for this yet; this page frames
the problem and the shape a solution would probably take.

---

## The gap

Every relationship XFTY generates today is a **reference**: a scalar
foreign-key-shaped property on the child pointing at the parent's `Id`
(`DefaultRelationship`, wired by `LookupWiring`), or the mirror-image child
collection (`ChildProvider`). That models a relational row, or a NoSQL
document store used in *normalized* mode (a Mongo/Cosmos/Dynamo item that
references another item's key rather than containing it).

It does not model **embedding** - a document database's other common shape,
where the "natural" persisted form nests the related data directly inside the
parent (an `Account` document whose `contacts` field is an array of Contact
subdocuments, not a foreign key on a separate Contact collection). XFTY has
no relationship kind that says "generate this as nested data inside the
parent" instead of "generate this as a separate, FK-wired list."

## Why the existing seam doesn't fully cover it

`IPersistenceGateway.Insert(records, idField)` is free to do anything with
the flat, FK-wired lists XFTY already produces - including grouping children
by their FK and nesting them into the parent document before writing. That
covers a consumer willing to write that reshaping once per relationship, in
their own gateway, today, with zero engine changes. It does not cover:

- **Reading an embedded shape back out of a bundle** the way `Inject`/
  `BundleEnricher`/`GetChildList` do for a reference relationship - those all
  assume the generated graph itself is flat (separate lists per type), so a
  test asserting against the *pre-persistence* in-memory graph has no
  equivalent for "the embedded array on the parent."
- **A Provider declaring "this one embeds"** as part of its Master Template,
  the way `PutRequired`/`PutOptional`/`With(...)` declare a reference or
  child relationship today. Without that, embedding is entirely the
  gateway's problem to reconstruct from FK data, which works but pushes all
  of the relationship's shape knowledge out of the Provider and into
  persistence code - the opposite of where XFTY normally keeps it.

## What a real design would need

Sketched, not committed to:

- A third relationship kind alongside `IDefaultRelationship` (reference) and
  the `ChildProvider` collection - something like `IEmbeddedRelationship` -
  that `AncestorGenerator`/a new `EmbeddedGenerator` materializes as a
  property *value* on the parent (a `List<Contact>` or similar) rather than
  a separate bundle entry wired by FK.
- A decision on how `Bundle`/`GetList`/`GetChildList` represent an embedded
  result - probably a new bundle accessor rather than overloading the
  existing FK-shaped ones, since "the children are already sitting on the
  parent object" and "the children are a separate list the bundle also
  tracks" are genuinely different shapes to hand back to a test.
- Working out `Inject`/`BundleEnricher`'s role once some relationships are
  already embedded at generation time and don't need grafting back on for
  the code under test to see them - likely a no-op for that relationship,
  which is a simplification, not new complexity, if the rest of the design
  holds together.

## Not blocked on anything

Unlike the persistence-gateway work, this doesn't need a new project or an
external dependency - it's a pure engine/API design question. The main risk
is scope creep: get the one-hop, single-level embedding case solid before
considering nested embedding (a document embedding documents that themselves
embed further documents).

See also: [reference/known-issues.md](../reference/known-issues.md),
[roadmap/README.md](README.md).
