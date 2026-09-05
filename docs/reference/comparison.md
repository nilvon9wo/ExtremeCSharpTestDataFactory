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
| **AutoFixture** | Eliminate "Arrange" boilerplate in a unit test by auto-populating every property/constructor argument with an anonymous, deliberately-meaningless value ("it doesn't matter what this is, only that it's present"). Auto-mocking integration (AutoMoq, AutoNSubstitute) for dependencies. |
| **Bogus** | Generate *realistic-looking* fake data — names, addresses, emails, lorem ipsum, commerce/finance data, many locales — via an explicit per-property rule (`Faker<T>().RuleFor(...)`). |
| **AutoBogus** | AutoFixture's auto-population philosophy plus Bogus's realistic generators, picked by convention (a property named `Email` gets a fake email). |
| **NBuilder** | A fluent way to stamp out *N* similar objects with per-item overrides and simple sequential values. The simplest tool here. |
| **XFTY** | Generate a **graph** of related records for a domain model that has real relationships - required/optional, shared, deferred - with a per-call choice of whether that graph gets mocked, left alone, or actually inserted through a pluggable persistence seam. |

---

## Feature comparison

| Capability | AutoFixture | Bogus | AutoBogus | NBuilder | XFTY |
|---|:---:|:---:|:---:|:---:|:---:|
| Realistic fake data out of the box (names, emails, addresses, lorem ipsum) | ❌ (deliberately not) | ✅ | ✅ | ❌ | ❌ (bring your own `IValueExpression`, or a literal) |
| Auto-populates every property with no rules written | ✅ | ❌ (rule per property) | ✅ | ❌ | ❌ (a Provider states its own defaults) |
| Fluent per-record override of specific fields | ✅ (`.With(...)` via customizations) | ✅ (`RuleFor`) | ✅ | ✅ (`.With(...)`) | ✅ (`Put`/override templates) |
| Recursive nested-object population | ✅ | manual (write a rule per nested object) | ✅ | ❌ | ✅ (relationship generation) |
| Required vs. optional relationship control, per call | ❌ | ❌ | ❌ | ❌ | ✅ (`InsertInclusivity`, per-call `IncludeOptional`/`ExcludeRelationship`) |
| One shared parent instance reused/deduplicated across many generated children | ❌ | ❌ | ❌ | ❌ | ✅ (`SharedAncestor`) |
| Self-referential / circular relationship cycle guard | partial (depth limit) | n/a | partial (depth limit) | n/a | ✅ (explicit cycle detection + opt-out) |
| A field's value derived from a sibling/ancestor/descendant, with a mis-ordering guard | ❌ | partial (a rule sees the in-progress object; no ordering guard, no ancestor/descendant graph walk) | ❌ | ❌ | ✅ (`IContextAwareExpression`, loud on misuse) |
| Choosing a different "recipe" for the same type by a runtime condition | partial (global customizations) | manual (separate `Faker<T>` instances) | ❌ | ❌ | ✅ (`FlavouredLookupKey`/`DiscriminatorLookupKey`, resolved per relationship) |
| Insert-mode abstraction (generate only vs. mock-Id vs. actually persist) | ❌ (out of scope) | ❌ (out of scope) | ❌ (out of scope) | ❌ (out of scope) | ✅ (`InsertMode`, pluggable `IPersistenceGateway`) |
| Dependency-ordered batch insert across mixed record types | ❌ | ❌ | ❌ | ❌ | ✅ (`DepthBatchedInserter`) |
| Graft a generated graph's relationships onto records an `init`-only model rejects (for code under test that reads `.Account.Name`) | ❌ | ❌ | ❌ | ❌ | ✅ (`Inject`/`RecordInjector`) |
| Auto-mocking of service dependencies (not data) | ✅ (AutoMoq/AutoNSubstitute) | ❌ | ❌ | ❌ | ❌ (out of scope - generates data records, not service doubles) |
| Deep extensibility hook (custom generation strategy per type) | ✅ (`ISpecimenBuilder`) | ✅ (custom rules) | ✅ | limited | ✅ (`IRecordProvider`, custom `IValueExpression`/`IContextAwareExpression`/`IDefaultRelationship`) |
| Maturity / ecosystem | very mature, long track record | mature, widely used | smaller, less active in recent years | older, largely superseded by the above | **new — first beta, no production track record yet** |

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
  what happens to it next is entirely your own code.
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

- **No realistic fake data.** This is the single biggest, most practical gap
  against Bogus/AutoBogus. XFTY ships structural value expressions
  (`IncrementingStringExpression`, `UniqueEmailExpression`, `LiteralExpression`,
  the `CopyFrom*` family) but nothing that produces a plausible human name,
  street address, or paragraph of body text. A Provider that wants that has
  to supply its own `IValueExpression` - trivially possible (wrap a call to
  Bogus, even), but not bundled.
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
  and a real database:** XFTY. Nothing above does this; combine it with
  Bogus for the realistic-value gap (a `Faker<Address>` call inside an
  `IValueExpression` composes fine).

See also: [reference/known-issues.md](known-issues.md),
[roadmap/README.md](../roadmap/README.md).
