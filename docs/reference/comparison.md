# How XFTY Compares to Other .NET Test-Data Libraries

An honest comparison, not a sales pitch. These tools solve overlapping but
genuinely different problems; several of them are more mature, more widely
used, and better at what *they* do than XFTY is at the same narrow task.
Where XFTY has a real edge, it's a narrow one: **generating a related,
constraint-valid object *graph*, with explicit control over whether and how
it gets persisted** — not filling in one object's properties.

Compared here: [AutoFixture](https://github.com/AutoFixture/AutoFixture),
[Bogus](https://github.com/bchavez/Bogus),
[AutoBogus](https://github.com/nickdodd79/AutoBogus), and
[NBuilder](https://github.com/nbuilder/nbuilder). Maintenance-status and
popularity claims below reflect this port's author's knowledge as of early
2026 - check each project's own repository for current activity before
relying on that part.

---

## What each one is actually *for*

| | Primary job |
|---|---|
| **XFTY** | Generate a **graph** of related records for a domain model that has real relationships - required/optional, shared, deferred - with a per-call choice of whether that graph gets mocked, left alone, or actually inserted through a pluggable persistence seam. |
| **AutoFixture** | Eliminate "Arrange" boilerplate in a unit test by auto-populating every property/constructor argument with an anonymous, deliberately-meaningless value ("it doesn't matter what this is, only that it's present"). Auto-mocking integration (AutoMoq, AutoNSubstitute) for dependencies. |
| **Bogus** | Generate *realistic-looking* fake data — names, addresses, emails, lorem ipsum, commerce/finance data, many locales — via an explicit per-property rule (`Faker<T>().RuleFor(...)`). |
| **AutoBogus** | AutoFixture's auto-population philosophy plus Bogus's realistic generators, picked by convention (a property named `Email` gets a fake email). |
| **NBuilder** | A fluent way to stamp out *N* similar objects with per-item overrides and simple sequential values. The simplest tool here. |

---

## Feature comparison

GitHub can't render a wide table without forcing a horizontal scrollbar, so
this one stays to symbols only — the two prose sections right below it
("real edge" / "genuinely loses") carry the actual explanation for every row
that needs one. Rows are grouped by whether XFTY has the capability at all
(gaps at the bottom), then sorted most-common to least-common within each
group, by how many of the five tools have it.

✅ yes · ❌ no · ◐ partial · — not applicable

| Capability | XFTY | AutoFixture | Bogus | AutoBogus | NBuilder |
|---|:---:|:---:|:---:|:---:|:---:|
| Fluent per-record override of specific fields | ✅ | ✅ | ✅ | ✅ | ✅ |
| Deep extensibility hook (custom strategy per type) | ✅ | ✅ | ✅ | ✅ | ◐ |
| Recursive nested-object population | ✅ | ✅ | ◐ | ✅ | ❌ |
| Self-referential / circular relationship cycle guard | ✅ | ◐ | — | ◐ | — |
| Runtime-conditioned recipe choice for the same type | ✅ | ◐ | ◐ | ❌ | ❌ |
| Sibling/ancestor/descendant-derived value, with a mis-ordering guard | ✅ | ❌ | ◐ | ❌ | ❌ |
| Required vs. optional relationship control, per call | ✅ | ❌ | ❌ | ❌ | ❌ |
| Shared parent deduplicated across many children | ✅ | ❌ | ❌ | ❌ | ❌ |
| Insert-mode abstraction (mock vs. actually persist) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Dependency-ordered batch insert across mixed types | ✅ | ❌ | ❌ | ❌ | ❌ |
| Graft onto an `init`-only model a real constructor rejects | ✅ | ❌ | ❌ | ❌ | ❌ |
| Realistic fake data | ◐ | ❌ | ✅ | ✅ | ❌ |
| Auto-populates every property, no rules written | ❌ | ✅ | ❌ | ✅ | ❌ |
| Auto-mocking of service dependencies (not data) | ❌ | ✅ | ❌ | ❌ | ❌ |
| Maturity / ecosystem *(not a capability — kept last, unsorted)* | new — first beta | very mature | mature | smaller, less active recently | older, largely superseded |

---

## Where XFTY has a real edge

- **It models a graph, not an object.** Every tool above generates one thing
  (recursively, in AutoFixture's/AutoBogus's case) per `Create`/`RuleFor`
  call. XFTY generates a *primary record plus its required and optional
  relationships*, with per-call control over how deep that goes, and dedupes
  a shared parent across many children instead of creating one per child.
  None of the alternatives have an equivalent to `SharedAncestor` or to
  `InsertInclusivity`.
- **It has an opinion about persistence, without committing to one
  technology.** `InsertMode` + `IPersistenceGateway` mean the same Provider
  definitions serve a pure-in-memory unit test (`Mock`) and a real database
  integration test (`Now`, through EF Core, Dapper, or anything else) with
  no rewrite. The other four tools stop at "here's your populated object";
  what happens to it next is entirely your own code. `DepthBatchedInserter`
  extends this to inserting a mixed-type graph in dependency order, one
  batch per depth level, instead of one call per object.
- **It can graft onto a model none of the alternatives can populate.** An
  `init`-only or constructor-validated model that rejects a builder pattern
  outright is still reachable via `Inject`/`RecordInjector`, which sets the
  relationship through reflection after the fact - useful for code under
  test that reads `.Account.Name` off a type your test can't otherwise wire.
- **Context-aware values are a first-class, guarded concept.** A field
  derived from a sibling, an ancestor several hops up, or (once the whole
  graph exists) a child, with a *loud* error on a mis-ordered read instead of
  a silently wrong `null`. Bogus gets partway there (a rule can see the
  object being built) but has no ancestor/descendant graph to read from and
  no ordering guard.
- **Variant resolution is relationship-aware.** `FlavouredLookupKey`/
  `DiscriminatorLookupKey` let *a relationship* pick a different Provider
  variant for its target based on a key or a predicate - not just "which
  customization is globally active right now."

## Where XFTY genuinely loses

- **No realistic fake data in the core package.** Core `Xfty` ships
  structural value expressions (`IncrementingStringExpression`,
  `UniqueEmailExpression`, `LiteralExpression`, the `CopyFrom*` family) but
  nothing that produces a plausible human name, street address, or paragraph
  of body text - `Xfty.Bogus` (`FakeFullNameExpression`,
  `FakeEmailAddressExpression`, `FakeStreetAddressExpression`,
  `FakeParagraphExpression`) closes that gap as a separate, opt-in package
  instead, so the base library never depends on Bogus.
- **No auto-population.** Every field a Provider cares about has to be
  declared somewhere (a Master Template default, an override template, a
  `Put(...)`). AutoFixture's/AutoBogus's "just fill in everything, I'll tell
  you what matters" model is a genuinely different, and for many unit tests
  simpler, default.
- **No auto-mocking story.** AutoFixture's AutoMoq/AutoNSubstitute integration
  solves a different-but-adjacent problem (faking *dependencies*, not
  *data*) that XFTY doesn't touch at all.
- **A bigger API to learn.** `Fixture.Create<T>()` or
  `new Faker<T>().RuleFor(...)` is a one-line mental model. XFTY's Providers,
  Master Templates, lookup keys, insert modes, and relationship inclusivity
  are a real API surface - proportionate to the graph-shaped problem it
  solves, but genuinely more to learn for a test that just needs one
  populated object.
- **No track record.** AutoFixture and Bogus have a decade-plus of
  production use between them. XFTY is a first beta release of a from-scratch
  C# port; treat it accordingly.

## When to reach for which

- **One object, don't care what's in it:** AutoFixture.
- **One object, needs to *look* real** (a demo, a UI screenshot, believable
  seed data): Bogus or AutoBogus.
- **A short, disposable list of similar objects with a couple of tweaks:**
  NBuilder, or just a LINQ `Select` - none of these tools are required for
  something this simple.
- **A whole related graph — parents, optional relationships, shared
  ancestors, a validation rule that needs the graph to actually be
  constraint-valid — and you want the exact same test to run against a mock
  and a real database:** XFTY. Nothing above does this; add `Xfty.Bogus` for
  the realistic-value gap (`FakeFullNameExpression`, `FakeEmailAddressExpression`,
  and friends compose fine as ordinary `IValueExpression`s).

## Could XFTY pair with one of these to close a gap?

Sometimes, and it's worth being specific about which gap and how much work
each pairing actually takes - "compose" is doing very different amounts of
work in each case below.

- **Bogus, for realistic values — done, as a separate package.** An
  `IValueExpression` is just an interface; nothing stops it from calling a
  `Faker<T>` and returning the result. `Xfty.Bogus` bundles the common cases
  (`FakeFullNameExpression`, `FakeEmailAddressExpression`,
  `FakeStreetAddressExpression`, `FakeParagraphExpression`) so most Providers
  never have to write that wrapper themselves; writing a custom one for
  anything Bogus offers that isn't bundled still works exactly the same way.
- **AutoFixture / AutoBogus, for auto-population — a real gap, and a real
  design, not a trivial fix.** The gap is genuine: a Provider must declare
  every field it cares about, where AutoFixture's model is "fill in
  everything, then tell me what you overrode." Closing it *conveniently*
  (not just "call `Fixture.Create<T>()` yourself before handing the object
  to XFTY," which already works but means XFTY's own relationship/ancestor
  logic never sees or touches those fields) would need a genuine
  integration point - a fallback hook a `RecordProvider` calls for any
  field neither a Master Template, an override template, nor a relationship
  set, backed by an injected `ISpecimenBuilder` (or a Bogus `Faker<T>`).
  That's a small, separate adapter project (`Xfty.AutoFixture`), not a
  core change - tracked as an idea, not committed:
  [autofixture-fallback-fill.md](../roadmap/autofixture-fallback-fill.md).
- **NBuilder — not really a gap to close.** XFTY already generates *N*
  similar records natively (call a Provider's `Supply()` in a loop, or use
  `With`/`WithChildren` for the nested case); NBuilder's own niche is
  already inside what XFTY does, just with more ceremony for the trivial
  case. Pairing them adds a dependency without closing anything.
- **Auto-mocking (AutoMoq/AutoNSubstitute) — a different problem, not a gap
  XFTY has.** These fake *service dependencies* your code under test calls;
  XFTY generates *data records*. A test that needs both just uses each tool
  for its own concern independently - there's no shared surface for them to
  compose across, so there's nothing to build here.

See also: [reference/known-issues.md](known-issues.md),
[roadmap/README.md](../roadmap/README.md).
