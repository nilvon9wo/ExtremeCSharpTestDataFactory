using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>RecordProvider&lt;TRecord&gt; - per-call configuration setters.</summary>
public sealed partial class RecordProvider<TRecord>
{
    public RecordProvider<TRecord> SetQuantityPerTemplate(int quantityPerListedTemplate)
    {
        _ = this._inner.SetQuantityPerTemplate(quantityPerListedTemplate);
        return this;
    }

    public RecordProvider<TRecord> SetOverrideTemplateList(List<object> overrideTemplateList)
    {
        _ = this._inner.SetOverrideTemplateList(overrideTemplateList);
        return this;
    }

    public RecordProvider<TRecord> SetOverrideTemplate(object overrideTemplate)
    {
        _ = this._inner.SetOverrideTemplate(overrideTemplate);
        return this;
    }

    public RecordProvider<TRecord> WithVariant(ILookupKey variantKey)
    {
        _ = this._inner.WithVariant(variantKey);
        return this;
    }

    public RecordProvider<TRecord> SetInsertMode(InsertMode insertMode)
    {
        _ = this._inner.SetInsertMode(insertMode);
        return this;
    }

    public RecordProvider<TRecord> SetMockIdGenerator(IMockIdGenerator mockIdGenerator)
    {
        _ = this._inner.SetMockIdGenerator(mockIdGenerator);
        return this;
    }

    public RecordProvider<TRecord> SetInclusivity(InsertInclusivity inclusivity)
    {
        _ = this._inner.SetInclusivity(inclusivity);
        return this;
    }

    public RecordProvider<TRecord> SetPersistenceGateway(IPersistenceGateway gateway)
    {
        _ = this._inner.SetPersistenceGateway(gateway);
        return this;
    }

    public RecordProvider<TRecord> SetUnsetFieldFiller(IUnsetFieldFiller filler)
    {
        _ = this._inner.SetUnsetFieldFiller(filler);
        return this;
    }

    public RecordProvider<TRecord> AllowAncestorCycles()
    {
        _ = this._inner.AllowAncestorCycles();
        return this;
    }

    public RecordProvider<TRecord> ExcludePrimaryIds()
    {
        _ = this._inner.ExcludePrimaryIds();
        return this;
    }

    public RecordProvider<TRecord> IncludePrimaryIds()
    {
        _ = this._inner.IncludePrimaryIds();
        return this;
    }

    public RecordProvider<TRecord> DepthBatched()
    {
        _ = this._inner.DepthBatched();
        return this;
    }

    public RecordProvider<TRecord> ForceStructuralChildGeneration()
    {
        _ = this._inner.ForceStructuralChildGeneration();
        return this;
    }
}