# XFTY Architecture

This document describes the internal architecture of XFTY and the design
decisions behind it.

Most users only need the public API in [../use/](../use/) and
[../extend/](../extend/). This guide is for developers who want to understand
how the framework works internally, or contribute to it.

---

# Design Goals

XFTY is first and foremost a **test data factory**.

Its goal is to make test setup:

- concise
- maintainable
- declarative
- reusable

To achieve this, XFTY includes a small engine responsible for constructing
complete object graphs, applying default values, generating related records,
and optionally persisting them.

The engine exists so that test code doesn't have to.

A typical test should describe only the data it actually cares about, while
XFTY supplies everything else.

---

# High-Level Architecture

```text
Tests
   │
   ▼
RecordProvider
   │
   ▼
Provider Lookup
   │
   ▼
IRecordProvider
   │
   ▼
MasterTemplate
   │
   ▼
RecordFactory
   │
   ▼
Bundle
```

Each component has a single responsibility.

| Component | Namespace | Responsibility |
|-----------|-----------|-----------------|
| `RecordProvider` | `Core` | Public fluent API used by tests. |
| `IProviderLookup` | `Lookup` | Resolves which Provider should generate a particular record. |
| `DefaultProviderLookup` | `Demo` | Copy-me starter implementation (also this port's own self-test lookup). |
| `ProviderLookups` | `Lookup` | Reusable lookup mechanics, so a project's lookup stays a few one-liners over a `Dictionary`. |
| `ILookupKey` / `LookupKey` / `FlavouredLookupKey` | `Lookup` | Identifies a Provider variant (record type, optionally + predicate-matched flavour). |
| `IRecordProvider` | `Core` | Describes how one record type should be generated. |
| `MasterTemplate` | `Core` | Declarative description of default values and relationships. |
| `GenerationContext` | `Core` | The per-run state the engine threads everywhere: Provider Lookup, insert mode, inclusivity, forced-relationship paths, `BatchedInsertPending`, and — during the value pass — the record being built, its ancestor bundle, and the field currently being generated (`ValueFieldPass`). |
| `RecordFactory` | `Engine` | Thin coordinator — drives the phase classes below. |
| `AncestorGenerator` | `Engine` | Phase: generate one level of related (ancestor) records. |
| `LookupWiring` | `Engine` | Phase: point each record's lookup fields at its generated parents. |
| `PlainValueFiller` | `Engine` | Phase: fill the plain (`IValueExpression`) values. |
| `ContextAwareValuePass` | `Engine` | Phase: run the `IContextAwareExpression` values, one field at a time. |
| `DescendantValuePass` / `DeferredGraph` | `Engine` | Up-flow value pass at the top of a deferred flatten: fill each `IDeferredExpression` (`CopyFromDescendantExpression`) from that record's now-generated children, read through the collected parent links. |
| `ValueFieldPass` | `Core` | The narrowest scope — one context-aware field + the set of sibling context-aware fields not yet generated (drives `context.SiblingValue`'s loud guard). |
| `RelationshipForcer` | `Engine` | Applies `IncludeOptional(...)` / `Put(path,...)` relationship-prefix paths to a per-call copy of the Master Template. |
| `PathValue` / `PathValueApplier` | `Core` / `Engine` | A `Put(List<PropertyInfo>, value)` override targeted at a generated ancestor; the applier lands the at-target ones on the level's template. |
| `ChildProvider` | `Core` | Config for one downward child collection (`With(...)` / `WithChildren(...)`); builds the child Provider + templates, recursively for grandchildren. |
| `SharedRelationshipWiring` | `Engine` | Wires a `SharedAncestor` (one resolved record, every child pointed at it). |
| `SharedAncestorResolver` | `Engine` | The pre-phase for shared ancestors: collect (dependency-ordered, nested, cycle guards) → generate `Never` → depth-batched resolve per sub-graph. Runs for every configured `SharedAncestor`; talks only to `SharedAncestorProvider`. |
| `SharedAncestorProvider` | `Relationships` | The single recipe for one shared ancestor's record — key ± override template plus the same per-record API a generated parent takes. |
| `RecordCloneFactory` | `Engine` | Deep-clones templates so no two generated records share an instance. |
| `IndexedRecord` | `Persistence` | An `(index, record)` pair — records are identified by position, since two generated records can be equal by value. |
| `DepthBatchedInserter` | `Persistence` | Kahn-style layered resolution: one pass per dependency depth. |
| `DeferredInserter` / `DeferredInsertBuffer` | `Persistence` | The `Deferred` registry and its bundle-walk; `Flush(gateway)` runs `DepthBatchedInserter` over the union through the given `IPersistenceGateway`. |
| `Bundle` | `Core` | Represents the generated graph. |
| `IValueExpression` / `IContextAwareExpression` / `IDeferredExpression` | `Values` | Expression interfaces for generating field values (plain / context-aware / up-flow). |
| `IDefaultRelationship` / `DefaultRelationship` / `ISharedRelationship` / `SharedAncestor` | `Relationships` | Interfaces + implementations for generating related records. |
| `IRecordPredicate` + `Field{EqualTo,GreaterThan,LessThan,InSet}Predicate` / `ValueComparison` / `{AllOf,AnyOf,Negation}Predicate` / `FieldPredicateFactory` + `PredicateFactory` (facades) | `Predicates` | Conditions a flavoured key matches a record against — one small class per operator, no branching. |
| `IdMocker` | `Persistence` | Generates unique placeholder Ids without persistence. |

Keeping these responsibilities separate makes each component relatively small
and easy to reason about.

---

# Declarative Rather Than Imperative

One of the fundamental design goals was to avoid imperative construction of
test data.

Instead of writing code such as:

```csharp
Account account = new() { /* ... */ };
account.Id = SomehowPersist(account);

Contact contact = new() { AccountId = account.Id };
SomehowPersist(contact);
```

Providers instead declare *what* should exist.

```csharp
new MasterTemplate(Field.Of<Account>(x => x.Id))
    .Put(Field.Of<Account>(x => x.Name), new IncrementingStringExpression("Account"))
    .PutRequired(Field.Of<Account>(x => x.OwnerId), new DefaultRelationship(new User()));
```

The framework is responsible for determining *how* that object graph should
be created.

---

# Master Templates

The `MasterTemplate` class is the declarative heart of XFTY.

A Master Template describes:

- default field values
- context-aware and deferred (up-flow) values
- required relationships
- optional relationships

Internally these are stored in maps keyed by `PropertyInfo`, plus an explicit
field-order list (a `Dictionary`'s enumeration order is not a contract to rely
on, and a context-aware value may read an earlier one).

---

# Why Relationships Are Keyed by `PropertyInfo`

Relationships are intentionally keyed by **the property that stores the lookup
value**, not by the target type. This tells XFTY exactly which field needs to
be populated, keeps graph construction and navigation consistent, and
naturally supports multiple relationships to the same record type (e.g. a
Contact's `AccountId` and a self-referencing `Account.ParentId` both resolve
independently even though both eventually point at an `Account`).

---

# Object Graphs

The Factory constructs complete object graphs rather than isolated records.
Each relationship is represented by its own nested `Bundle`, preserving the
recursive structure of the generated graph.

```csharp
bundle.GetList(Field.Of<Contact>(x => x.AccountId))
```

or

```csharp
bundle.GetBundle(Field.Of<Contact>(x => x.AccountId))
```

depending on whether a caller needs the related records themselves or the
entire subgraph beneath them.

---

# Graph construction phases

`RecordFactory` is a thin coordinator; each phase is its own class. For one
Provider's records:

0. **Shared ancestors** (`SharedAncestorResolver`, from
   `RecordProvider.SupplyBundle()` → `SharedAncestorResolver.ResolveAllConfigured`)
   — every `SharedAncestor` configured so far in the process is collected
   (dependency-ordered, following nested shared ancestors), generated in memory
   (`Never`), and resolved one depth-batched pass per sub-graph, **before**
   step 1, honouring the call's insert mode. Flat ancestors (Provider has no
   relationships) collapse to a single record.
1. **Ancestors** (`AncestorGenerator`) — recursively generate one level of
   related records. A relationship named in an `IncludeOptional(...)` /
   `Put(path, ...)` path is generated here whatever the inclusivity, and
   *fully formed*.
2. **Id assignment** — depending on the insert mode the records are given mock
   Ids (`Mock`), left Id-less, or inserted here for real (`Now`, one operation
   per level, through the configured `IPersistenceGateway` - throws without
   one).
3. **Lookup wiring** (`LookupWiring`) — once parents have Ids, point each
   child's lookup fields at them.
4. **Plain value pass** (`PlainValueFiller`).
5. **Context-aware value pass** (`ContextAwareValuePass`) — below.
6. **Up-flow value pass** (`DescendantValuePass`) — only when a deferred graph
   is flattened, once every record exists. Fields with an `IDeferredExpression`
   are left unresolved by phase 5 and filled here from that record's collected
   children. A non-batched build that carries one throws in phase 5 instead.

## Value passes

Field values are filled in **two in-line passes plus one deferred pass**, so an
expression can be aware of the rest of the record:

1. **Plain values** — the `IValueExpression`s, in the order the fields were
   `Put`.
2. **Context-aware values** — the `IContextAwareExpression`s, after the
   ancestor records exist and lookups are wired. Each is handed a
   `GenerationContext` scoped to its record (`RecordBeingBuilt`, `BundleSoFar`,
   `RowIndex`) and to the one field being generated (`ValueFieldPass`, which
   also carries the set of context-aware fields not yet reached).

A context-aware value therefore sees all plain values, all wired lookups, and
any context-aware value `Put` before it. Reading a *later* context-aware
value, or a circular pair, throws from `context.SiblingValue(field)` — naming
both fields and the `Put` order that fixes it.

3. **Up-flow values** — the `IDeferredExpression`s. A field on a generated
   *child* cannot be read in-line — the child does not exist yet — so these
   are left unresolved and filled by `DescendantValuePass` when a deferred
   graph is flattened, reading the child through
   `DeferredGraph.ChildrenOf(index, field)` over the parent links
   `DeferredInsertBuffer` collected.

---

# The Generation Context

Every step of one `Supply*()` call needs the same run-wide state: the Provider
Lookup, the insert mode, the relationship inclusivity, the forced-relationship
paths, and the `BatchedInsertPending` flag. These travel together as a
`GenerationContext` rather than as separate arguments. `GenerationContext` is
immutable; derive a new one with `ForRelated`/`ForRecord`/`ForValueField`
rather than mutating.

The context is also where `context.ForRelated()`'s **recursion transform**
lives:

| Parent context | Child context | Why |
|----------------|---------------|-----|
| `Inclusivity = PreventCascade` | `Inclusivity = None` | The direct relationships are generated, but they do not generate their own — the cascade stops one level down. |
| `ExcludePrimaryIds = true` | `ExcludePrimaryIds = false`, always | Excluding a primary from persistence is a property of *this call's own output*, never of an ancestor - an ancestor is always persisted exactly as the configured `InsertMode` already says, regardless of what the record referencing it opted out of. See [use/insert-modes.md](../use/insert-modes.md#excluding-the-primary---excludeprimaryids). |
| anything else | unchanged | |

`InsertMode` itself is never transformed here - `ExcludePrimaryIds` used to
be baked into two extra `InsertMode` values (`RelatedOnly`/`MockRelatedOnly`)
that this exact transform mapped to `Now`/`Mock` respectively. Pulling the
concept out into its own orthogonal flag removed the need for that
substitution entirely: an ancestor just inherits `InsertMode` unchanged,
the same as it always did for every other mode.

---

# Immutability

XFTY clones templates aggressively (`RecordCloneFactory`). Whenever records
are generated, the framework creates new instances rather than modifying
shared objects, avoiding accidental sharing between generated records.

---

# Mock Id Generation

`IdMocker` generates a simple unique string Id without any persistence step —
this port's replacement for Apex's realistic-Salesforce-Id-shaped mock, since
nothing downstream here parses the Id's format the way Salesforce's key
prefixes do.

---

# Provider Lookup

Rather than using a global registry, XFTY requires callers to explicitly
provide a Provider Lookup. This allows different applications to define
different Provider collections without a shared mutable registry.

---

# Why `PrimaryTargetField` Exists

Not every record type identifies records the same way. Rather than assuming
every generated object can be identified by `Id`, Providers explicitly expose
their primary target field — used when retrieving generated records from
Bundles, wiring relationships, and identifying the Provider's primary output.

---

# Design Trade-offs

Several implementation decisions intentionally favour simplicity over maximum
flexibility:

- by default every child receives its own generated parent
  ([shared ancestors](../use/shared-ancestors.md) opt out of this);
- relationship generation is controlled by broad inclusion modes, with
  per-call exceptions
  ([IncludeOptional / ExcludeRelationship](../use/per-call-relationships.md));
- resolution is one pass per Provider by default (`.DepthBatched()` /
  `Deferred` would collapse it, once a real `Now` exists).

---

# Final Thoughts

XFTY intentionally separates *describing* test data from *constructing* test
data. Tests remain focused on the behaviour being verified. Providers
describe valid business objects. The Factory constructs complete graphs.
Bundles preserve those graphs. This separation keeps the public API compact
while allowing the internal engine to handle the complexity.
