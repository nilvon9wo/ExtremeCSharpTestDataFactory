namespace Net.NowhereAtAll.Xfty.Lookup;

/// <summary>
/// Identifies which Provider variant should generate a particular record.
///
/// The default key (<see cref="LookupKey"/>) is just a record type,
/// reproducing the original one-Provider-per-type behaviour. A refined key
/// adds a discriminator - arbitrary predicates on the record
/// (<see cref="FlavouredLookupKey"/>), or a custom implementation.
///
/// A single record can match several registered keys, so
/// <see cref="IProviderLookup.KeysFor"/> returns a set; the most specific
/// match (<see cref="Specificity"/>) wins.
///
/// Keys are compared by <see cref="HashKey"/> rather than by identity, so two
/// different instances describing the same variant resolve to the same
/// Provider.
/// </summary>
public interface ILookupKey
{
    /// <summary>The record type this key selects a Provider for.</summary>
    Type RecordType { get; }

    /// <summary>
    /// Whether <paramref name="record"/> belongs to the variant this key
    /// describes. Used to derive a key from a relationship's override
    /// template when none was supplied explicitly.
    /// </summary>
    bool IsInstanceOf(object? record);

    /// <summary>Value-equality identity. Two keys with the same hash key are the same key.</summary>
    string HashKey { get; }

    /// <summary>How specific this key is; higher wins when several keys match one record. Plain type key = 0.</summary>
    int Specificity { get; }
}
