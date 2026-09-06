using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core;

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
    private readonly Type recordType;
    private readonly IProviderLookup providerLookup;
    private readonly RecordProviderTemplateConfig templateConfig;
    private readonly RecordProviderChildConfig childConfig = new();

    private List<object>? overrideTemplateList;
    private ILookupKey? explicitVariantKey;
    private int quantityPerListedTemplate = 1;
    private InsertMode insertMode = InsertMode.Never;
    private InsertInclusivity inclusivity = InsertInclusivity.None;
    private bool ancestorCyclesAllowed;
    private bool excludePrimaryIds;
    private bool depthBatched;
    private bool forceStructuralChildGeneration;
    private IPersistenceGateway? persistenceGateway;
    private IUnsetFieldFiller? unsetFieldFiller;
    private IRecordProvider? factoryOutlet;

    public RecordProvider(Type recordType, IProviderLookup providerLookup)
    {
        this.recordType = recordType ?? throw new XftyConfigurationException("A record type is required to request data.");
        this.providerLookup = providerLookup ?? throw new XftyConfigurationException("A Provider Lookup is required to request data.");
        this.templateConfig = new RecordProviderTemplateConfig(() => this.ResolveFactoryOutlet().MasterTemplate.Copy());
    }

    /// <summary>Convenience: start from a lookup key. The record type is taken from the key, pinned as the variant.</summary>
    public RecordProvider(ILookupKey variantKey, IProviderLookup providerLookup)
        : this(TypeOf(variantKey), providerLookup) =>
        this.explicitVariantKey = variantKey;

    /// <summary>Convenience: start from an override template. The record type is taken from the template.</summary>
    public RecordProvider(object overrideTemplate, IProviderLookup providerLookup)
        : this([overrideTemplate], providerLookup)
    {
    }

    /// <summary>Convenience: start from a list of override templates. The record type is taken from the first template.</summary>
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
        this.factoryOutlet ??= this.providerLookup.Get(this.ResolveVariantKey());

    /// <summary>
    /// Which Provider variant to use: an explicit key from WithVariant(...),
    /// or the key derived from the first override template, or the plain
    /// record-type key. Only consulted the first time the Provider is
    /// resolved.
    /// </summary>
    private ILookupKey ResolveVariantKey()
    {
        object? firstTemplate = this.overrideTemplateList is { Count: > 0 } ? this.overrideTemplateList[0] : null;
        ILookupKey? reconciled = ProviderLookups.Reconcile(this.providerLookup, this.explicitVariantKey, firstTemplate);
        return reconciled ?? LookupKey.Get(this.recordType);
    }

    private void AssertNoRecordTypeConflict(List<object>? overrideTemplateList)
    {
        object? conflicting = FirstConflictingTemplate(overrideTemplateList, this.recordType);
        if (conflicting is not null)
        {
            throw new RecordProviderConflictException($"This Provider requests {this.recordType} but was given a {conflicting.GetType()} override template.");
        }
    }

    private static object? FirstConflictingTemplate(List<object>? overrideTemplateList, Type recordType) =>
        overrideTemplateList?.FirstOrDefault(overrideTemplate => overrideTemplate.GetType() != recordType);
}
