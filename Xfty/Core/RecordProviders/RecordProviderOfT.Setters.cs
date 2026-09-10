using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>RecordProvider&lt;TRecord&gt; - the per-call configuration setters (everything except field and child config). Each forwards to the identically-named <see cref="RecordProvider"/> method, which carries the documentation.</summary>
public sealed partial class RecordProvider<TRecord>
{
    public RecordProvider<TRecord> SetQuantityPerTemplate(int quantityPerListedTemplate) =>
        this.Forwarding(() => this._inner.SetQuantityPerTemplate(quantityPerListedTemplate));

    public RecordProvider<TRecord> SetOverrideTemplateList(List<object> overrideTemplateList) =>
        this.Forwarding(() => this._inner.SetOverrideTemplateList(overrideTemplateList));

    public RecordProvider<TRecord> SetOverrideTemplate(object overrideTemplate) =>
        this.Forwarding(() => this._inner.SetOverrideTemplate(overrideTemplate));

    public RecordProvider<TRecord> WithVariant(ILookupKey variantKey) =>
        this.Forwarding(() => this._inner.WithVariant(variantKey));

    public RecordProvider<TRecord> SetInsertMode(InsertMode insertMode) =>
        this.Forwarding(() => this._inner.SetInsertMode(insertMode));

    public RecordProvider<TRecord> SetMockIdGenerator(IMockIdGenerator mockIdGenerator) =>
        this.Forwarding(() => this._inner.SetMockIdGenerator(mockIdGenerator));

    public RecordProvider<TRecord> SetInclusivity(InsertInclusivity inclusivity) =>
        this.Forwarding(() => this._inner.SetInclusivity(inclusivity));

    public RecordProvider<TRecord> SetPersistenceGateway(IPersistenceGateway gateway) =>
        this.Forwarding(() => this._inner.SetPersistenceGateway(gateway));

    public RecordProvider<TRecord> SetUnsetFieldFiller(IUnsetFieldFiller filler) =>
        this.Forwarding(() => this._inner.SetUnsetFieldFiller(filler));

    public RecordProvider<TRecord> AllowAncestorCycles() =>
        this.Forwarding(this._inner.AllowAncestorCycles);

    public RecordProvider<TRecord> ExcludePrimaryIds() =>
        this.Forwarding(this._inner.ExcludePrimaryIds);

    public RecordProvider<TRecord> IncludePrimaryIds() =>
        this.Forwarding(this._inner.IncludePrimaryIds);

    public RecordProvider<TRecord> DepthBatched() =>
        this.Forwarding(this._inner.DepthBatched);

    public RecordProvider<TRecord> ForceStructuralChildGeneration() =>
        this.Forwarding(this._inner.ForceStructuralChildGeneration);
}