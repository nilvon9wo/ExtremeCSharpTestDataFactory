using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Lookup;

/// <summary>Resolves which Provider should generate a given record.</summary>
public interface IProviderLookup
{
    /// <summary>Convenience for the common case; equivalent to Get(LookupKey.Get(sObjectType)).</summary>
    IRecordProvider Get(Type sObjectType);

    /// <summary>Resolve a Provider for an explicit variant key.</summary>
    IRecordProvider Get(ILookupKey lookupKey);

    /// <summary>Every registered key whose IsInstanceOf(record) is true.</summary>
    ISet<ILookupKey> KeysFor(object? record);
}
