using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// The primary entry point for the library: configure a record's fields and
/// relationships, then Supply()/SupplyList()/SupplyBundle() it. A thin fluent
/// facade split across several files by concern - field/relationship
/// configuration in RecordProvider.FieldConfig.cs (delegating to
/// <see cref="RecordProviderTemplateConfig"/>), setters in
/// RecordProvider.Setters.cs, child-collection generation in
/// RecordProvider.Children.cs (delegating to
/// <see cref="RecordProviderChildConfig"/>), and the Supply*() pipeline in
/// RecordProvider.Supply.cs. This file holds identity: construction and
/// working out which Provider/variant/template this call resolves to.
/// </summary>
public sealed partial class RecordProvider
{
    private readonly Type _recordType;
    private readonly IProviderLookup _providerLookup;
    private readonly RecordProviderTemplateConfig _templateConfig;
    private readonly RecordProviderChildConfig _childConfig = new();

    private List<object>? _overrideTemplateList;
    private ILookupKey? _explicitVariantKey;
    private int _quantityPerListedTemplate = 1;
    private InsertMode _insertMode = InsertMode.Never;
    private InsertInclusivity _inclusivity = InsertInclusivity.None;
    private bool _ancestorCyclesAllowed;
    private bool _excludePrimaryIds;
    private bool _depthBatched;
    private bool _forceStructuralChildGeneration;
    private IPersistenceGateway? _persistenceGateway;
    private IUnsetFieldFiller? _unsetFieldFiller;
    private IRecordProvider? _factoryOutlet;

    public RecordProvider(Type recordType, IProviderLookup providerLookup)
    {
        this._recordType = recordType ?? throw new XftyConfigurationException(
            "A record type is required to request data."
        );
        this._providerLookup = providerLookup ?? throw new XftyConfigurationException(
            "A Provider Lookup is required to request data."
        );
        this._templateConfig = new RecordProviderTemplateConfig(
            () => this.ResolveFactoryOutlet().MasterTemplate.Copy()
        );
    }

    /// <summary>
    /// Convenience: start from a lookup key. The record type is taken from the key, pinned as the variant.
    /// </summary>
    public RecordProvider(ILookupKey variantKey, IProviderLookup providerLookup)
        : this(TypeOf(variantKey), providerLookup) =>
        this._explicitVariantKey = variantKey;

    /// <summary>Convenience: start from an override template. The record type is taken from the template.</summary>
    public RecordProvider(object overrideTemplate, IProviderLookup providerLookup)
        : this([overrideTemplate], providerLookup)
    {
    }

    /// <summary>
    /// Convenience: start from a list of override templates. The record type is taken from the first template.
    /// </summary>
    public RecordProvider(List<object> overrideTemplateList, IProviderLookup providerLookup)
        : this(TypeOf(overrideTemplateList), providerLookup) =>
        this.SetOverrideTemplateList(overrideTemplateList);

    private static Type TypeOf(ILookupKey variantKey) =>
        (variantKey ?? throw new XftyConfigurationException("A lookup key is required to request data.")).RecordType;

    private static Type TypeOf(List<object>? overrideTemplateList)
    {
        bool hasAFirstTemplate = overrideTemplateList is { Count: > 0 } && overrideTemplateList[0] is not null;
        return hasAFirstTemplate ? overrideTemplateList![0].GetType() : throw NoTemplateToDeriveTypeFrom();
    }

    private static XftyConfigurationException NoTemplateToDeriveTypeFrom() =>
        new(
            "Cannot derive a record type from an empty or null template list - supply at least one concrete "
            + "template, or use the (Type, lookup) constructor.");

    private IRecordProvider ResolveFactoryOutlet() =>
        this._factoryOutlet ??= this._providerLookup.Get(this.ResolveVariantKey());

    /// <summary>
    /// Which Provider variant to use: an explicit key from WithVariant(...),
    /// or the key derived from the first override template, or the plain
    /// record-type key. Only consulted the first time the Provider is
    /// resolved.
    /// </summary>
    private ILookupKey ResolveVariantKey()
    {
        object? firstTemplate = this._overrideTemplateList is { Count: > 0 } ? this._overrideTemplateList[0] : null;
        ILookupKey? reconciled =
            ProviderLookups.Reconcile(this._providerLookup, this._explicitVariantKey, firstTemplate);
        return reconciled ?? LookupKey.Get(this._recordType);
    }

    private void AssertNoRecordTypeConflict(List<object>? overrideTemplateList)
    {
        object? conflicting = FirstConflictingTemplate(overrideTemplateList, this._recordType);
        if (conflicting is not null)
        {
            throw new RecordProviderConflictException(
                $"This Provider requests {this._recordType} but was given a {conflicting.GetType()} override template."
            );
        }
    }

    private static object? FirstConflictingTemplate(List<object>? overrideTemplateList, Type recordType) =>
        overrideTemplateList?.FirstOrDefault(overrideTemplate => overrideTemplate.GetType() != recordType);
}