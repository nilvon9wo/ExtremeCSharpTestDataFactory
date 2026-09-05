using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Lookup;

/// <summary>
/// The reusable mechanics behind an <see cref="IProviderLookup"/>, so a
/// project's own lookup stays a handful of one-line delegations over an
/// explicit dictionary. Nothing here is stateful and nothing mutates a
/// lookup: you pass a complete map in, you get an answer out.
/// </summary>
public static class ProviderLookups
{
    // Resolving a Provider ---------------------------------------------------

    /// <summary>Look up (and lazily instantiate + cache) a Provider for key.</summary>
    public static IRecordProvider Get(
        Dictionary<ILookupKey, Type> providerTypeByKey,
        Dictionary<ILookupKey, IRecordProvider> instanceCache,
        ILookupKey key)
    {
        RequireKey(key);
        if (!instanceCache.ContainsKey(key))
        {
            if (!providerTypeByKey.TryGetValue(key, out Type? providerType))
            {
                throw NotRegistered(key);
            }

            instanceCache[key] = (IRecordProvider)Activator.CreateInstance(providerType)!;
        }

        return instanceCache[key];
    }

    /// <summary>Look up an already-constructed Provider for key.</summary>
    public static IRecordProvider Get(Dictionary<ILookupKey, IRecordProvider> providerByKey, ILookupKey key)
    {
        RequireKey(key);
        return providerByKey.TryGetValue(key, out IRecordProvider? provider)
            ? provider
            : throw NotRegistered(key);
    }

    // Deriving a key from a record ------------------------------------------

    /// <summary>The subset of registeredKeys whose IsInstanceOf(record) is true.</summary>
    public static ISet<ILookupKey> KeysFor(ISet<ILookupKey> registeredKeys, object? record)
    {
        object requiredRecord = record ?? throw new LookupException("A record is required to derive a lookup key.");
        return registeredKeys
            .Where(key => key.RecordType == requiredRecord.GetType() && key.IsInstanceOf(requiredRecord))
            .ToHashSet();
    }

    /// <summary>
    /// The single key to generate a parent from, given a lookup and a
    /// record: the most specific match, or the plain type key when nothing
    /// refined matched. Two equally-specific matches is an error - the
    /// caller must supply an explicit key.
    /// </summary>
    public static ILookupKey Resolve(IProviderLookup providerLookup, object? record)
    {
        ISet<ILookupKey> matches = providerLookup.KeysFor(record);
        return matches.Count == 0
            ? LookupKey.Get(record?.GetType())
            : BestOf(matches, record);
    }

    private static ILookupKey BestOf(ISet<ILookupKey> matches, object? record)
    {
        int topSpecificity = matches.Max(key => key.Specificity);
        List<ILookupKey> topTier = matches.Where(key => key.Specificity == topSpecificity).ToList();
        List<string> topTierHashes = topTier.Select(key => key.HashKey).Distinct().ToList();
        return topTierHashes.Count > 1
            ? throw new LookupException(
                $"Ambiguous Provider variant for {record?.GetType()}: {string.Join(", ", topTierHashes)}. Supply an explicit lookup key.")
            : topTier[0];
    }

    /// <summary>
    /// The single variant key to generate from, given an optional explicit
    /// key and an optional override template - the two ways a caller can
    /// name a variant.
    /// </summary>
    public static ILookupKey? Reconcile(IProviderLookup providerLookup, ILookupKey? explicitKey, object? overrideTemplate) =>
        (explicitKey, overrideTemplate) switch
        {
            (null, null) => null,
            (null, not null) => Resolve(providerLookup, overrideTemplate),
            _ when ContradictsTemplate(providerLookup, explicitKey, overrideTemplate) =>
                throw ContradictionException(providerLookup, explicitKey, overrideTemplate),
            _ => explicitKey,
        };

    private static bool ContradictsTemplate(IProviderLookup providerLookup, ILookupKey explicitKey, object? overrideTemplate)
    {
        if (overrideTemplate is null)
        {
            return false;
        }

        ILookupKey fromTemplate = Resolve(providerLookup, overrideTemplate);
        return fromTemplate.Specificity > 0 && fromTemplate.HashKey != explicitKey.HashKey;
    }

    private static LookupException ContradictionException(IProviderLookup providerLookup, ILookupKey explicitKey, object? overrideTemplate)
    {
        ILookupKey fromTemplate = Resolve(providerLookup, overrideTemplate);
        return new LookupException(
            $"Explicit variant {explicitKey.HashKey} contradicts the override template, which matches "
            + $"{fromTemplate.HashKey}. Supply only one.");
    }

    // Ready-made map-backed lookups ---------------------------------------

    /// <summary>A lookup over a complete map of already-constructed Providers.</summary>
    public static IProviderLookup Of(Dictionary<ILookupKey, IRecordProvider> providerByKey) =>
        new MapBackedLookup(null, providerByKey, null);

    /// <summary>As Of(Map), plus the shared-ancestor defaults the Providers rely on.</summary>
    public static IProviderLookup Of(
        Dictionary<ILookupKey, IRecordProvider> providerByKey,
        Dictionary<string, object> sharedAncestorDefaults) =>
        new MapBackedLookup(null, providerByKey, sharedAncestorDefaults);

    /// <summary>A lookup over a complete map of Provider types (instantiated lazily).</summary>
    public static IProviderLookup OfTypes(Dictionary<ILookupKey, Type> providerTypeByKey) =>
        new MapBackedLookup(providerTypeByKey, null, null);

    // ---------------------------------------------------------------------

    private static void RequireKey(ILookupKey? key) =>
        _ = key ?? throw new LookupException("A lookup key is required.");

    private static LookupException NotRegistered(ILookupKey key) =>
        new($"No data provider registered for {key.RecordType} (key: {key.HashKey}).");
}
