using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>RecordProvider - the per-call configuration setters (everything except field/child config).</summary>
public sealed partial class RecordProvider
{
    public RecordProvider SetQuantityPerTemplate(int quantityPerListedTemplate)
    {
        this.quantityPerListedTemplate = AssertPositive(quantityPerListedTemplate);
        return this;
    }

    private static int AssertPositive(int quantity) =>
        quantity >= 1 ? quantity : throw new XftyConfigurationException($"It makes no sense to supply {quantity}.");

    /// <summary>The record type set by the constructor always wins - a list of a different type throws.</summary>
    public RecordProvider SetOverrideTemplateList(List<object> overrideTemplateList)
    {
        this.AssertNoRecordTypeConflict(overrideTemplateList);
        this.overrideTemplateList = overrideTemplateList;
        return this;
    }

    public RecordProvider SetOverrideTemplate(object overrideTemplate) =>
        this.SetOverrideTemplateList([overrideTemplate]);

    /// <summary>Pin the Provider variant explicitly, instead of letting it be derived from the override template.</summary>
    public RecordProvider WithVariant(ILookupKey variantKey)
    {
        this.AssertTemplateNotYetCustomized();
        AssertVariantKeyMatchesType(variantKey, this.recordType);
        this.explicitVariantKey = variantKey;
        return this;
    }

    private void AssertTemplateNotYetCustomized()
    {
        if (this.templateConfig.HasCustomTemplate)
        {
            throw new XftyConfigurationException("Call WithVariant(...) before customizing the template with Put(...).");
        }
    }

    private static void AssertVariantKeyMatchesType(ILookupKey? variantKey, Type recordType)
    {
        ILookupKey key = variantKey ?? throw new XftyConfigurationException("A variant key is required.");
        if (key.RecordType != recordType)
        {
            throw new RecordProviderConflictException($"Variant key is for {key.RecordType} but this Provider requests {recordType}.");
        }
    }

    public RecordProvider SetInsertMode(InsertMode insertMode)
    {
        this.insertMode = insertMode;
        return this;
    }

    public RecordProvider SetInclusivity(InsertInclusivity inclusivity)
    {
        this.inclusivity = inclusivity;
        return this;
    }

    /// <summary>The real backing store InsertMode.Now saves through. Without one, Now throws.</summary>
    public RecordProvider SetPersistenceGateway(IPersistenceGateway gateway)
    {
        this.persistenceGateway = gateway;
        return this;
    }

    /// <summary>
    /// Opt in to filling fields this Provider's Master Template never
    /// configured at all - see <see cref="IUnsetFieldFiller"/>. Applies to
    /// every record this call generates, including ancestors pulled in
    /// along the way (each against its own Master Template's own unset
    /// fields). Xfty.AutoFixture bundles an AutoFixture-backed one.
    /// </summary>
    public RecordProvider SetUnsetFieldFiller(IUnsetFieldFiller filler)
    {
        this.unsetFieldFiller = filler;
        return this;
    }

    /// <summary>Suppress the ancestor-cycle guard for this call. Use only when the chain genuinely terminates on its own.</summary>
    public RecordProvider AllowAncestorCycles()
    {
        this.ancestorCyclesAllowed = true;
        return this;
    }

    /// <summary>
    /// Opt in to one insert per dependency depth for this Now call, instead
    /// of one per Provider. Not supported with shared ancestors resolved
    /// under manual mode.
    /// </summary>
    public RecordProvider DepthBatched()
    {
        this.depthBatched = true;
        return this;
    }

    /// <summary>Internal: a child of a DEFERRED/depth-batched parent must build its own children structurally too.</summary>
    public RecordProvider ForceStructuralChildGeneration()
    {
        this.forceStructuralChildGeneration = true;
        return this;
    }
}
