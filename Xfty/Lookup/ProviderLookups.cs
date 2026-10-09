using Net.NowhereAtAll.Xfty.Core.RecordProviders;
namespace Net.NowhereAtAll.Xfty.Lookup;

/// <summary>
/// The reusable mechanics behind an <see cref="IProviderLocating"/>, so a
/// project's own lookup stays a handful of one-line delegations over an
/// explicit dictionary. Nothing here is stateful and nothing mutates a
/// lookup: you pass a complete map in, you get an answer out.
/// </summary>
public static class ProviderLookups
{
    // Resolving a Provider ---------------------------------------------------

    /// <summary>lookup.Get(typeof(TRecord)), without the typeof.</summary>
    public static IRecordProviding Get<TRecord>(this IProviderLocating lookup) => lookup.Get(typeof(TRecord));

    /// <summary>Look up (and lazily instantiate + cache) a Provider for key.</summary>
    public static IRecordProviding Get(
        Dictionary<IRecordIdentifying, Type> providerTypeByKey,
        Dictionary<IRecordIdentifying, IRecordProviding> instanceCache,
        IRecordIdentifying key
    )
    {
        RequireKey(key);
        if (!instanceCache.TryGetValue(key, out IRecordProviding? cached))
        {
            if (!providerTypeByKey.TryGetValue(key, out Type? providerType))
            {
                throw NotRegistered(key);
            }

            cached = (IRecordProviding)Activator.CreateInstance(providerType)!;
            instanceCache[key] = cached;
        }

        return cached;
    }

    /// <summary>Look up an already-constructed Provider for key.</summary>
    public static IRecordProviding Get(
        Dictionary<IRecordIdentifying, IRecordProviding> providerByKey,
        IRecordIdentifying key
    )
    {
        RequireKey(key);
        return providerByKey.TryGetValue(key, out IRecordProviding? provider)
            ? provider
            : throw NotRegistered(key);
    }

    // Deriving a key from a record ------------------------------------------

    /// <summary>The subset of registeredKeys whose IsInstanceOf(record) is true.</summary>
    public static ISet<IRecordIdentifying> KeysFor(ISet<IRecordIdentifying> registeredKeys, object? record)
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
    public static IRecordIdentifying Resolve(IProviderLocating providerLookup, object? record)
    {
        ISet<IRecordIdentifying> matches = providerLookup.KeysFor(record);
        return matches.Count == 0
            ? LookupKey.Get(record?.GetType())
            : BestOf(matches, record);
    }

    private static IRecordIdentifying BestOf(ISet<IRecordIdentifying> matches, object? record)
    {
        int topSpecificity = matches.Max(key => key.Specificity);
        List<IRecordIdentifying> topTier = [.. matches.Where(key => key.Specificity == topSpecificity)];
        List<string> topTierHashes = [.. topTier.Select(key => key.HashKey).Distinct()];
        return topTierHashes.Count > 1
            ? throw new LookupException(
                $"Ambiguous Provider variant for {record?.GetType()}: "
                + $"{string.Join(", ", topTierHashes)}. Supply an explicit lookup key."
            )
            : topTier[0];
    }

    /// <summary>
    /// The single variant key to generate from, given an optional explicit
    /// key and an optional override template - the two ways a caller can
    /// name a variant.
    /// </summary>
    public static IRecordIdentifying? Reconcile(
        IProviderLocating providerLookup,
        IRecordIdentifying? explicitKey,
        object? overrideTemplate
    ) =>
        (explicitKey, overrideTemplate) switch
        {
            (null, null) => null,
            (null, not null) => Resolve(providerLookup, overrideTemplate),
            _ when ContradictsTemplate(providerLookup, explicitKey, overrideTemplate) =>
                throw ContradictionException(providerLookup, explicitKey, overrideTemplate),
            _ => explicitKey,
        };

    private static bool ContradictsTemplate(
        IProviderLocating providerLookup,
        IRecordIdentifying explicitKey,
        object? overrideTemplate
    )
    {
        if (overrideTemplate is null)
        {
            return false;
        }

        IRecordIdentifying fromTemplate = Resolve(providerLookup, overrideTemplate);
        return fromTemplate.Specificity > 0 && fromTemplate.HashKey != explicitKey.HashKey;
    }

    private static LookupException ContradictionException(
        IProviderLocating providerLookup,
        IRecordIdentifying explicitKey,
        object? overrideTemplate
    )
    {
        IRecordIdentifying fromTemplate = Resolve(providerLookup, overrideTemplate);
        return new LookupException(
            $"Explicit variant {explicitKey.HashKey} contradicts the override template, which matches "
            + $"{fromTemplate.HashKey}. Supply only one."
        );
    }

    // Ready-made map-backed lookups ---------------------------------------

    /// <summary>A lookup over a complete map of already-constructed Providers.</summary>
    public static IProviderLocating Of(Dictionary<IRecordIdentifying, IRecordProviding> providerByKey) =>
        new MapBackedLookup(null, providerByKey, null);

    /// <summary>As Of(Map), plus the shared-ancestor defaults the Providers rely on.</summary>
    public static IProviderLocating Of(
        Dictionary<IRecordIdentifying, IRecordProviding> providerByKey,
        Dictionary<string, object> sharedAncestorDefaults
    ) =>
        new MapBackedLookup(null, providerByKey, sharedAncestorDefaults);

    /// <summary>A lookup over a complete map of Provider types (instantiated lazily).</summary>
    public static IProviderLocating OfTypes(Dictionary<IRecordIdentifying, Type> providerTypeByKey) =>
        new MapBackedLookup(providerTypeByKey, null, null);

    // ---------------------------------------------------------------------

    private static void RequireKey(IRecordIdentifying? key) =>
        _ = key ?? throw new LookupException("A lookup key is required.");

    private static LookupException NotRegistered(IRecordIdentifying key) =>
        new($"No data provider registered for {key.RecordType} (key: {key.HashKey}).");
}