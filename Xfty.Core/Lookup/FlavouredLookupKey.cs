using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Lookup;

/// <summary>
/// Selects a Provider variant by record type and one or more arbitrary
/// predicates on the record ("annual revenue over 1M", "industry in a set").
/// The "flavour" string labels the combination.
///
/// IsInstanceOf(record) is true when the type matches and every predicate is
/// satisfied. A flavour with no predicates can only be used explicitly.
///
/// Instances are flyweights, interned by record type + flavour (predicates
/// are *not* part of the identity). Obtain one with <see cref="Get"/> and add
/// its predicates with <see cref="Matching"/> **once**, in a single place.
///
/// The Apex original also carried a record-type discriminator
/// (Salesforce RecordType); that's genuinely Salesforce-specific schema
/// metadata with no C# analog (a real target would be EF's TPH discriminator
/// column, not attempted here) - dropped rather than faked.
/// </summary>
public sealed class FlavouredLookupKey : ILookupKey
{
    private static readonly Dictionary<string, FlavouredLookupKey> InstanceByHash = new();

    private readonly LookupKey baseKey;
    private readonly List<IRecordPredicate> predicates = [];

    private string _flavour { get; }

    private FlavouredLookupKey(Type sObjectType, string flavour)
    {
        this.baseKey = LookupKey.Get(sObjectType);
        this._flavour = flavour;
    }

    public static FlavouredLookupKey Get(Type sObjectType, string flavour)
    {
        string hash = HashOf(LookupKey.Get(sObjectType), flavour);
        if (!InstanceByHash.TryGetValue(hash, out FlavouredLookupKey? existing))
        {
            existing = new FlavouredLookupKey(sObjectType, flavour);
            InstanceByHash[hash] = existing;
        }

        return existing;
    }

    /// <summary>Add a condition the record must satisfy to belong to this flavour. Chainable.</summary>
    public FlavouredLookupKey Matching(IRecordPredicate predicate)
    {
        this.predicates.Add(predicate);
        return this;
    }

    public Type SObjectType => this.baseKey.SObjectType;

    public bool IsInstanceOf(object? record) =>
        this.predicates.Count > 0
        && this.baseKey.IsInstanceOf(record)
        && this.predicates.All(predicate => predicate.IsSatisfiedBy(record));

    public string HashKey => HashOf(this.baseKey, this._flavour);

    // More specific than the plain type key, and more so with more predicates.
    public int Specificity => 20 + this.predicates.Count;

    public override bool Equals(object? other) =>
        other is ILookupKey otherKey && otherKey.HashKey == this.HashKey;

    public override int GetHashCode() => this.HashKey.GetHashCode();

    private static string HashOf(LookupKey baseKey, string flavour) =>
        $"{baseKey.HashKey}::flavour={flavour}";
}
