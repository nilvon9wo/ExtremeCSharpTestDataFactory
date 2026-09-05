# Provider Lookups

A [Provider](providers.md) knows *what* type it generates. A **Provider Lookup**
knows *which Provider* generates a given type (or variant). **Every project
writes its own** — a small class holding a complete, explicit map of lookup key
→ Provider.

Why yours and not one XFTY ships: editing a class XFTY ships makes upgrades
painful, and your project's own Providers know your project's required fields
and relationships in a way a generic starter kit cannot.

---

## The pattern

`ProviderLookups` supplies the mechanics, so your class is a few one-liners:

```csharp
public sealed class MyProjectLookup : IProviderLookup
{
    private static readonly Dictionary<ILookupKey, Type> Providers = new()
    {
        [LookupKey.Get(typeof(Account))] = typeof(MyAccountProvider),
        [LookupKey.Get(typeof(Contact))] = typeof(MyContactProvider),
    };

    private readonly Dictionary<ILookupKey, IRecordProvider> cache = [];

    public IRecordProvider Get(Type sObjectType) => this.Get(LookupKey.Get(sObjectType));

    public IRecordProvider Get(ILookupKey key) => ProviderLookups.Get(Providers, this.cache, key);

    public ISet<ILookupKey> KeysFor(object? record) => ProviderLookups.KeysFor(Providers.Keys.ToHashSet(), record);
}
```

- Each registered Provider type needs a public no-arg constructor. For Providers
  that need constructor arguments, use
  `ProviderLookups.Of(Dictionary<key, providerInstance>)`.
- `ProviderLookups.OfTypes(map)` / `Of(map)` also wrap a complete map directly
  for quick or in-test use, returning a ready-made `IProviderLookup`.
- Lookup keys compare by value (`HashKey`), so they work as dictionary keys
  directly. Obtain them with `LookupKey.Get(...)`, never `new`.

`DefaultProviderLookup` (`Net.Nowhereatall.Xfty.Demo`) is exactly this pattern
with this port's own two Providers — the framework uses it for its own
self-tests, and it is the class to copy as a starting point.

---

## The three methods

| Method | Returns |
|--------|---------|
| `Get(Type)` | the Provider for the plain type |
| `Get(ILookupKey)` | the Provider for a specific variant |
| `KeysFor(object? record)` | every registered key the record matches (a record can match more than one) |

`ProviderLookups.Resolve(lookup, record)` turns a `KeysFor` match set into the
single most-specific key.

---

Registering more than one Provider per type (flavours):
[provider-variants](provider-variants.md).
