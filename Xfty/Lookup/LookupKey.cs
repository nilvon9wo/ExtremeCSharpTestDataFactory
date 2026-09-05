using Net.Nowhereatall.Xfty.Core;
namespace Net.Nowhereatall.Xfty.Lookup;

/// <summary>
/// The default lookup key: a record type and nothing else. Using only this
/// key reproduces XFTY's original behaviour - exactly one Provider per
/// record type.
///
/// Instances are flyweights - obtain them with <see cref="Get(Type?)"/>,
/// never <c>new</c> (the constructor is private).
/// </summary>
public sealed class LookupKey : ILookupKey
{
    private static readonly Dictionary<Type, LookupKey> InstanceByType = [];

    private LookupKey(Type recordType) => this.RecordType = recordType;

    public static LookupKey Get(Type? recordType)
    {
        Type type = recordType ?? throw new XftyConfigurationException("A lookup key requires a record type.");
        if (!InstanceByType.TryGetValue(type, out LookupKey? existing))
        {
            existing = new LookupKey(type);
            InstanceByType[type] = existing;
        }

        return existing;
    }

    public static LookupKey Get(object? record) =>
        Get(record?.GetType());

    public Type RecordType { get; }

    public bool IsInstanceOf(object? record) =>
        record is not null && record.GetType() == this.RecordType;

    // Value equality by hash key, so lookup keys work as dictionary keys directly.
    public string HashKey => this.RecordType.ToString();

    public int Specificity => 0;

    public override bool Equals(object? other) =>
        other is ILookupKey otherKey && otherKey.HashKey == this.HashKey;

    public override int GetHashCode() => this.HashKey.GetHashCode();
}
