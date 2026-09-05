# Design: Multi-Variant Providers

Status: **implemented**, condensed from the Apex original's design record. The
goal — resolve a Provider by more than a bare record type — carried over; the
Salesforce Record Type discriminator did not (no C# analog).

---

## Goal

Resolve a Provider by more than its record type. The motivating case: two
genuinely different Master Templates for one type (e.g. a VIP vs. a standard
Account), where a plain `IProviderLookup.Get(Type)` only allows one Provider
per type.

## As shipped

- **The lookup is a plain map + a stateless utility, not a registry.** A
  project's `IProviderLookup` holds a complete, explicit
  `Dictionary<ILookupKey, Type>` (or `..., IRecordProvider>`) and delegates
  its methods to `ProviderLookups`. No mutation, no "last wins".
  `DefaultProviderLookup` is that pattern with this port's own Providers.
- **All keys are flyweights**, obtained with `.Get(...)`, compared by
  `HashKey` so two different instances describing the same variant collide
  correctly in a `Dictionary`.
- **`FlavouredLookupKey`** — record type + arbitrary `IRecordPredicate`
  conditions (`FieldPredicateFactory` ships the common single-field ones).
  Interned by type + flavour; predicates are added once via `.Matching(...)`,
  typically in a `*LookupKeys` constants class both the Provider Lookup and
  the pinning relationships reference.
- **`KeysFor` returns a set** (a record can match several variants);
  `ProviderLookups.Resolve` picks the most specific via `ILookupKey.Specificity`
  (0 for the plain type key, 20+ for a flavoured key); an equally-specific tie
  is an error.
- **Deferred + memoised key resolution** —
  `IDefaultRelationship.ResolveLookupKey(lookup)` runs when the factory needs
  it, not at construction time, since a relationship template doesn't know
  what variants exist yet when it's built.
- **`Required` + `Optional` merged** into one `DefaultRelationship`;
  requiredness lives in the Master Template slot (`PutRequired` /
  `PutOptional`); a relationship passed to plain `Put` is rejected.

## Not ported

Salesforce's `RecordTypeLookupKey` (a record-type discriminator resolved via
schema describe) and `RecordTypeDataProvider` (its backing SOQL repository)
have no C# equivalent and are not ported — see
[reference/known-issues](../reference/known-issues.md). `FlavouredLookupKey`'s
predicate-based matching is this port's only variant mechanism.

## Backwards-compatible surface

`Get(Type)` still works (it's sugar over `Get(LookupKey.Get(type))`); a
relationship built from a bare override template (no explicit key) still
derives its variant from `ProviderLookups.Resolve` against the template.
