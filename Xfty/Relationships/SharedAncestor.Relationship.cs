using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>SharedAncestor - the IDefaultRelationship/ISharedRelationship surface a Master Template puts it as.</summary>
public sealed partial class SharedAncestor
{
    public object? OverrideTemplate => this._source?.OverrideTemplate();

    public PropertyInfo? RelatedField => this._source?.RelatedField();

    public ILookupKey? ResolveLookupKey(IProviderLookup providerLookup) => this.Source().LookupKey(providerLookup);

    public bool IsResolved => this._resolvedRecord is not null;

    public async Task<object?> ResolveSharedRecord(GenerationContext context)
    {
        if (Disabled.ContainsKey(this.SharedName))
        {
            return null;
        }

        object? record = this._resolvedRecord ?? await this.ResolveFresh(context).ConfigureAwait(false);
        this.LearnPrimaryFieldFrom(context.ProviderLookup);
        return record;
    }

    private Task<object?> ResolveFresh(GenerationContext context) =>
        s_manualResolution ? this.ResolveUnderManualMode(context) : this.ResolveAllThenReturnOwn(context);

    private async Task<object?> ResolveAllThenReturnOwn(GenerationContext context)
    {
        await SharedAncestorResolver.ResolveAllConfigured(context.ProviderLookup, context.InsertMode).ConfigureAwait(false);
        return (await this.ResolveNow(context.ProviderLookup, context.InsertMode).ConfigureAwait(false))._resolvedRecord;
    }

    private async Task<object?> ResolveUnderManualMode(GenerationContext context) =>
        this.Source().IsLightweight(context.ProviderLookup)
            ? (await this.ResolveNow(context.ProviderLookup, context.InsertMode).ConfigureAwait(false))._resolvedRecord
            : throw this.NoAutoResolutionException();

    private XftyConfigurationException NoAutoResolutionException() =>
        new(
            $"Shared ancestor \"{this.SharedName}\" has a sub-graph of its own and auto-resolution is off (manual "
            + $"resolution only). Resolve it up front: SharedAncestor.Get(\"{this.SharedName}\").ResolveNow(lookup, mode), "
            + "or SharedAncestor.ResolveNow(lookup, mode, names).");
}