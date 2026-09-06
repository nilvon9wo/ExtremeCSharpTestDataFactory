# Auto-population fallback via AutoFixture

Status: built, as a separate package (`Xfty.AutoFixture`). See
[use/autofixture.md](../use/autofixture.md) for how to use it - this page is
now the design-history record, not an open idea.

## The gap

Every field a Provider wants set has to be declared somewhere - a Master
Template default, an override template, a `Put(...)`, or a relationship.
AutoFixture's (and AutoBogus's) model inverts that: populate every property
automatically, and a test only says what it *overrides*. For a test that
truly doesn't care about most of a record's shape, that's less to write.

Calling `Fixture.Create<T>()` yourself before handing the object to a
Provider already worked before this package existed, but it didn't really
pair the two tools - it just ran them in sequence, and XFTY's
relationship/ancestor/context-aware logic never saw or influenced whatever
AutoFixture filled in (a generated Account's Id, for instance, got
overwritten anyway once XFTY assigned it). See
[reference/comparison.md](../reference/comparison.md#could-xfty-pair-with-one-of-these-to-close-a-gap)
for the fuller comparison.

## What shipped

Two independent, non-mutually-exclusive integrations - both proven in
`Xfty.AutoFixture.Test`, documented in
[use/autofixture.md](../use/autofixture.md):

1. **`XftyCustomization`/`XftySpecimenBuilder`** - an `ICustomization` that
   points `fixture.Create<T>()` at a registered `RecordProvider` instead of
   AutoFixture's own generation, for any `T` with a Provider in the given
   lookup. This direction turned out to need no core `Xfty` change at all -
   `ISpecimenBuilder` is exactly the right shape (`object Create(object
   request, ISpecimenContext context)`; return `NoSpecimen` to fall through
   to AutoFixture's default generation for anything unregistered).
2. **`IUnsetFieldFiller`/`AutoFixtureUnsetFieldFiller`** - the fallback-fill
   direction this page originally sketched. This one *did* need a real core
   change:
   - `MasterTemplate.IsConfigured(PropertyInfo)` - whether a field has a
     default value, a context-aware value, a deferred value, a required/
     optional relationship, or is the primary target field itself. The one
     piece of new information the rest of the design depends on: a field
     nothing configured is distinguishable from a field XFTY resolved *to*
     null or some other value on purpose.
   - `IUnsetFieldFiller`, a new interface in core `Xfty` (`Core/`,
     no dependency on AutoFixture or anything else) with one method,
     `Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields)`.
   - `RecordProvider.SetUnsetFieldFiller(...)`, threaded through
     `GenerationContext` (a new `UnsetFieldFiller` property, following the
     exact pattern `PersistenceGateway` already used) so it reaches every
     record a `Supply*()` call generates, ancestors included - each against
     its own Provider's own unset fields, via `AncestorGenerator`'s existing
     `context.ForRelated(...)` derivation.
   - `RecordFactory.Build(...)` calls it once per record, after every
     value/relationship pass (`ContextAwareValuePass`, deferred-value
     registration) but before `Persist(...)` - matching this page's
     original sketch of "late enough... but before the record is handed to
     `IPersistenceGateway`" exactly.
   - `AutoFixtureUnsetFieldFiller` (in `Xfty.AutoFixture`) resolves each
     unset field through the given `IFixture`'s own specimen-builder
     pipeline (`new SpecimenContext(fixture).Resolve(field.PropertyType)`),
     catches `ObjectCreationException` for a field that circles back on its
     own type under the fixture's default `ThrowingRecursionBehavior`
     (leaving it as XFTY left it, rather than letting one bad field abort
     the whole `Supply()` call), and supports `.Excluding(field)` for
     properties that should never be auto-filled - see
     [use/autofixture.md](../use/autofixture.md#excluding-specific-fields)
     for why a navigation-shaped property (`Contact.Account`,
     `Account.Contacts` - populated by `Bundle.Inject(...)`, never by a
     Master Template) is the case that comes up in practice.

## Why the original sketch's `IUnsetFieldFiller` shape survived unchanged

The original sketch's own wording - "given a record instance and the set of
`PropertyInfo`s XFTY did *not* set, fill in the rest" - turned out to need
almost no revision once actually built. The one addition beyond that
sketch: propagating the filler through `GenerationContext` so it reaches
generated ancestors automatically, which the original note didn't
anticipate needing (it only considered the top-level record a
`RecordProvider.Supply()` call returns).
