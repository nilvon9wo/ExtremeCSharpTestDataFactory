using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>What an <see cref="IMockIdGenerator"/> is told about the record it is minting an Id for.</summary>
public sealed class MockIdContext(Type recordType, PropertyInfo idField, object record)
{
    public Type RecordType { get; } = recordType;

    public PropertyInfo IdField { get; } = idField;

    /// <summary>The record instance itself, so a generator can build an Id from its other field values if it needs to.</summary>
    public object Record { get; } = record;
}