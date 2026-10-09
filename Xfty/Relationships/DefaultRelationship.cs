using System.Reflection;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>
/// The standard relationship implementation: generate a fresh parent record
/// from the given override template.
/// </summary>
public sealed class DefaultRelationship(
    IRecordIdentifying? lookupKey,
    object? overrideTemplate,
    PropertyInfo? relatedField
) : IRelatable
{
    private readonly IRecordIdentifying? _explicitLookupKey = lookupKey;
    private IRecordIdentifying? _resolvedLookupKey;

    public DefaultRelationship(object? overrideTemplate) : this(null, overrideTemplate, null)
    {
    }

    public DefaultRelationship(object? overrideTemplate, PropertyInfo? relatedField)
        : this(null, overrideTemplate, relatedField)
    {
    }

    public DefaultRelationship(IRecordIdentifying? lookupKey, object? overrideTemplate)
        : this(lookupKey, overrideTemplate, null)
    {
    }

    public object? OverrideTemplate { get; } = overrideTemplate;

    public PropertyInfo? RelatedField { get; } = relatedField;

    public IRecordIdentifying? ResolveLookupKey(IProviderLocating providerLookup)
    {
        this._resolvedLookupKey ??= ProviderLookups.Reconcile(
            providerLookup,
            this._explicitLookupKey,
            this.OverrideTemplate
        );
        return this._resolvedLookupKey;
    }
}