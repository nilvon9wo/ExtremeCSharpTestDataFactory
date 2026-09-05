using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Relationships;

/// <summary>SharedAncestor - the IDefaultRelationship/ISharedRelationship surface a Master Template puts it as.</summary>
public sealed partial class SharedAncestor
{
    public object? OverrideTemplate => this.source?.OverrideTemplate();

    public PropertyInfo? RelatedField => this.source?.RelatedField();

    public ILookupKey? ResolveLookupKey(IProviderLookup providerLookup) => this.Source().LookupKey(providerLookup);

    public string SharedName => this._name;

    public bool IsResolved => this.resolvedRecord is not null;

    public bool IsResolvedRecordPersisted => this._resolvedRecordIsPersisted;

    public object? ResolveSharedRecord(GenerationContext context) =>
        Disabled.Contains(this._name)
            ? null
            : this.resolvedRecord ?? this.ResolveFresh(context);

    private object? ResolveFresh(GenerationContext context) =>
        _manualResolution ? this.ResolveUnderManualMode(context) : this.ResolveAllThenReturnOwn(context);

    private object? ResolveAllThenReturnOwn(GenerationContext context)
    {
        SharedAncestorResolver.ResolveAllConfigured(context.ProviderLookup, context.InsertMode);
        return this.ResolveNow(context.ProviderLookup, context.InsertMode).resolvedRecord;
    }

    private object? ResolveUnderManualMode(GenerationContext context) =>
        this.Source().IsLightweight(context.ProviderLookup)
            ? this.ResolveNow(context.ProviderLookup, context.InsertMode).resolvedRecord
            : throw this.NoAutoResolutionException();

    private XftyConfigurationException NoAutoResolutionException() =>
        new(
            $"Shared ancestor \"{this._name}\" has a sub-graph of its own and auto-resolution is off (manual "
            + $"resolution only). Resolve it up front: SharedAncestor.Get(\"{this._name}\").ResolveNow(lookup, mode), "
            + "or SharedAncestor.ResolveNow(lookup, mode, names).");
}
