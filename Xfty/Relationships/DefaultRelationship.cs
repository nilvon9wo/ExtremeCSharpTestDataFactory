using System.Reflection;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Relationships;

/// <summary>
/// The standard relationship implementation: generate a fresh parent record
/// from the given override template.
/// </summary>
public sealed class DefaultRelationship : IDefaultRelationship
{
    private readonly ILookupKey? explicitLookupKey;
    private ILookupKey? resolvedLookupKey;

    public DefaultRelationship(object? overrideTemplate) : this(null, overrideTemplate, null)
    {
    }

    public DefaultRelationship(object? overrideTemplate, PropertyInfo? relatedField) : this(null, overrideTemplate, relatedField)
    {
    }

    public DefaultRelationship(ILookupKey? lookupKey, object? overrideTemplate) : this(lookupKey, overrideTemplate, null)
    {
    }

    public DefaultRelationship(ILookupKey? lookupKey, object? overrideTemplate, PropertyInfo? relatedField)
    {
        this.explicitLookupKey = lookupKey;
        this.OverrideTemplate = overrideTemplate;
        this.RelatedField = relatedField;
    }

    public object? OverrideTemplate { get; }

    public PropertyInfo? RelatedField { get; }

    public ILookupKey? ResolveLookupKey(IProviderLookup providerLookup)
    {
        this.resolvedLookupKey ??= ProviderLookups.Reconcile(providerLookup, this.explicitLookupKey, this.OverrideTemplate);
        return this.resolvedLookupKey;
    }
}
