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

    public async Task<object?> ResolveSharedRecord(GenerationContext context) =>
        Disabled.ContainsKey(this._name)
            ? null
            : this.resolvedRecord ?? await this.ResolveFresh(context);

    private Task<object?> ResolveFresh(GenerationContext context) =>
        _manualResolution ? this.ResolveUnderManualMode(context) : this.ResolveAllThenReturnOwn(context);

    private async Task<object?> ResolveAllThenReturnOwn(GenerationContext context)
    {
        await SharedAncestorResolver.ResolveAllConfigured(context.ProviderLookup, context.InsertMode);
        return (await this.ResolveNow(context.ProviderLookup, context.InsertMode)).resolvedRecord;
    }

    private async Task<object?> ResolveUnderManualMode(GenerationContext context) =>
        this.Source().IsLightweight(context.ProviderLookup)
            ? (await this.ResolveNow(context.ProviderLookup, context.InsertMode)).resolvedRecord
            : throw this.NoAutoResolutionException();

    private XftyConfigurationException NoAutoResolutionException() =>
        new(
            $"Shared ancestor \"{this._name}\" has a sub-graph of its own and auto-resolution is off (manual "
            + $"resolution only). Resolve it up front: SharedAncestor.Get(\"{this._name}\").ResolveNow(lookup, mode), "
            + "or SharedAncestor.ResolveNow(lookup, mode, names).");
}
