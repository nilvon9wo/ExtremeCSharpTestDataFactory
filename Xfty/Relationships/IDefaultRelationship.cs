using System.Reflection;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>
/// How a related record should be produced for a lookup field.
///
/// Whether the relationship is *required* or *optional* is not part of this
/// contract - that is decided by which slot it occupies on the Master
/// Template (PutRequired vs PutOptional).
/// </summary>
public interface IDefaultRelationship
{
    /// <summary>Override template for the generated parent; also identifies its record type.</summary>
    object? OverrideTemplate { get; }

    /// <summary>The parent field whose value is copied into the child's lookup field, or null to use the parent's Id.</summary>
    PropertyInfo? RelatedField { get; }

    /// <summary>
    /// The lookup key identifying which Provider variant generates the
    /// parent. Returns the explicit key if one was supplied, otherwise
    /// derives one from the override template. The result is memoised on
    /// first call.
    /// </summary>
    ILookupKey? ResolveLookupKey(IProviderLookup providerLookup);
}
