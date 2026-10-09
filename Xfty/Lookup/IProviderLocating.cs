using Net.NowhereAtAll.Xfty.Core.RecordProviders;
namespace Net.NowhereAtAll.Xfty.Lookup;

/// <summary>Resolves which Provider should generate a given record.</summary>
public interface IProviderLocating
{
    /// <summary>Convenience for the common case; equivalent to Get(LookupKey.Get(recordType)).</summary>
    IRecordProviding Get(Type recordType);

    /// <summary>Resolve a Provider for an explicit variant key.</summary>
    IRecordProviding Get(IRecordIdentifying lookupKey);

    /// <summary>Every registered key whose IsInstanceOf(record) is true.</summary>
    ISet<IRecordIdentifying> KeysFor(object? record);
}