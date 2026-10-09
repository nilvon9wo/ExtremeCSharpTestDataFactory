# Migration

This is a fresh C# port (`Net.NowhereAtAll.Xfty`) — there is no prior release
of it to migrate *from*. Apex's migration guide here covered breaking changes
between two Apex releases (3.5 → 4.0); none of that history applies to a
codebase that started from the 4.0-era Apex source as its one-time reference
point.

If you are coming from the **Apex original** rather than an earlier version of
this port, you are not migrating so much as reading a different API entirely —
see [salesforce-considerations](salesforce-considerations.md) for what carries
over and what doesn't, and [known-issues](known-issues.md) for the full list of
capability gaps (record types, org seeding, a real `Now` persistence layer, and
more).

Breaking changes between this port's own releases are listed below.

---

## Interfaces renamed to adjectives (after 1.0.0-beta.12)

Coding standard rule 11 names interfaces with adjectives describing what
implementers are able to do; every public interface was a noun. They were
renamed before 1.0, while a breaking change is still cheap. Members,
namespaces and the concrete classes are unchanged - only the interface
names (and their files) moved. A find-and-replace of whole words, in this
order-independent table, migrates a consumer:

| Before | After |
|---|---|
| `IRecordProvider` | `IRecordProviding` |
| `IProviderLookup` | `IProviderLocating` |
| `ILookupKey` | `IRecordIdentifying` |
| `IPersistenceGateway` | `IPersisting` |
| `IMockIdGenerator` | `IMockIdGenerating` |
| `IValueExpression` | `IValueYielding` |
| `IContextAwareExpression` | `IContextAware` |
| `IDeferredExpression` | `IDeferred` |
| `IRecordPredicate` | `IRecordMatching` |
| `IUnsetFieldFiller` | `IUnsetFieldFilling` |
| `IPathTargetValue` | `IPathApplicable` |
| `ISharedAncestorDefaults` | `ISharedAncestorRegistering` |
| `IDefaultRelationship` | `IRelatable` |
| `ISharedRelationship` | `ISharedRelatable` |

