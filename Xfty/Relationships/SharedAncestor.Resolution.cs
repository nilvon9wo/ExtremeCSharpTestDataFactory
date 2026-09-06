using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>SharedAncestor - resolving one instance's shared record, and reading it back afterwards.</summary>
public sealed partial class SharedAncestor
{
    /// <summary>Resolve now - e.g. to read GetId(name) before any Supply*() call.</summary>
    public async Task<SharedAncestor> ResolveNow(IProviderLookup lookup, InsertMode insertMode)
    {
        if (this.resolvedRecord is null)
        {
            SharedAncestorResolver.ApplyLookupDefaults(lookup);
            await new SharedAncestorResolver(lookup, insertMode).Resolve([this]);
        }

        return this;
    }

    public SharedAncestorProvider Source() =>
        this.source ?? throw new XftyConfigurationException(
            $"Shared ancestor \"{this._name}\" was never registered - call SharedAncestor.Put(\"{this._name}\", template / key).");

    /// <summary>The resolver hands back the generated record and its graph.</summary>
    public void AcceptResolved(object record, Bundle bundle, bool persisted)
    {
        this.resolvedRecord = record;
        this.resolvedBundle = bundle;
        this._resolvedRecordIsPersisted = persisted;
    }

    /// <summary>The shared record as a single-record sub-bundle. Never null once resolved.</summary>
    public Bundle GetResolvedBundle()
    {
        if (this.resolvedBundle is null && this.resolvedRecord is not null)
        {
            this.resolvedBundle = SingleRecordBundle(this.resolvedRecord);
        }

        return this.resolvedBundle!;
    }

    private static Bundle SingleRecordBundle(object record)
    {
        PropertyInfo idField = record.GetType().GetProperty("Id")!;
        Bundle bundle = new();
        bundle.PutPrimaries(idField, [record]);
        return bundle;
    }
}
