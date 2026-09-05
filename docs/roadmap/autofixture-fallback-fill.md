# Auto-population fallback via AutoFixture/AutoBogus

Status: idea, not designed.

## The gap

Every field a Provider wants set has to be declared somewhere - a Master
Template default, an override template, a `Put(...)`, or a relationship.
AutoFixture's (and AutoBogus's) model inverts that: populate every property
automatically, and a test only says what it *overrides*. For a test that
truly doesn't care about most of a record's shape, that's less to write.

Calling `Fixture.Create<T>()` yourself before handing the object to a
Provider already works today, but it doesn't really pair the two tools - it
just runs them in sequence, and XFTY's relationship/ancestor/context-aware
logic never sees or influences whatever AutoFixture filled in (a generated
Account's Id, for instance, gets overwritten anyway once XFTY assigns it).
See [reference/comparison.md](../reference/comparison.md#could-xfty-pair-with-one-of-these-to-close-a-gap)
for the fuller comparison.

## What a real integration would need

A fallback hook a `RecordProvider` consults for any field that ends up
untouched after its own Master Template, override template, and
relationship resolution have all run - late enough that AutoFixture never
fights XFTY for a field XFTY actually cares about, but before the record is
handed to `IPersistenceGateway`.

Sketch:

- A new optional collaborator, something like `IUnsetFieldFiller`, with one
  method: given a record instance and the set of `PropertyInfo`s XFTY did
  *not* set, fill in the rest.
- An adapter implementing it in a separate package (`Xfty.AutoFixture`),
  wrapping an injected `IFixture` - kept out of core `Xfty` so the base
  package never depends on AutoFixture.
- `RecordProvider`/`SimpleRecordProvider` would need a way to opt a
  particular Provider into this behavior - likely a constructor parameter
  or a `MasterTemplate` setting, not a global switch, since most Providers
  want every field to stay exactly what they declared.

## Why this isn't started

It is a real design, not a trivial fix: it touches where in the pipeline
"unset" is actually determined (a field a relationship sets to its
generated default vs. one nothing touched need to be distinguishable), and
it adds a genuinely new extension point rather than reusing an existing
one. Worth doing if a real project hits the auto-population gap often
enough to justify it - not committed to yet.
