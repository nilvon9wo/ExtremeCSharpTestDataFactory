using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
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
            await new SharedAncestorResolver(lookup, insertMode).Resolve([this]).ConfigureAwait(false);
        }

        this.LearnPrimaryFieldFrom(lookup);
        return this;
    }

    /// <summary>
    /// Pin down which property is the shared record's primary key - from the
    /// resolved bundle if one was built, otherwise from the Provider - so
    /// GetId(name) and the single-record bundle never fall back to guessing "Id".
    /// </summary>
    private void LearnPrimaryFieldFrom(IProviderLookup lookup)
    {
        this.resolvedPrimaryField ??= this.resolvedBundle?.PrimaryTargetField ?? this.PrimaryFieldFromProvider(lookup);

        // Only the PutAsValue path (no bundle) guessed persistence from an "Id"-named property; correct it now.
        bool cameFromPutAsValue = this.resolvedBundle is null && this.resolvedRecord is not null;
        if (cameFromPutAsValue && this.resolvedPrimaryField is not null)
        {
            this._resolvedRecordIsPersisted = this.resolvedPrimaryField.GetValue(this.resolvedRecord) is not null;
        }
    }

    /// <summary>The Provider's primary-key field, when the source has a template or variant key to resolve one from; null for a bare PutAsValue registration.</summary>
    private PropertyInfo? PrimaryFieldFromProvider(IProviderLookup lookup) =>
        this.source is { } theSource && theSource.CanResolvePrimaryField
            ? theSource.PrimaryField(lookup)
            : null;

    public SharedAncestorProvider Source() =>
        this.source ?? throw new XftyConfigurationException(
            $"Shared ancestor \"{this._name}\" was never registered - call SharedAncestor.Put(\"{this._name}\", template / key).");

    /// <summary>The resolver hands back the generated record and its graph.</summary>
    public void AcceptResolved(object record, Bundle bundle, bool persisted)
    {
        this.resolvedRecord = record;
        this.resolvedBundle = bundle;
        this.resolvedPrimaryField = bundle.PrimaryTargetField;
        this.resolvedMockIdGenerator = bundle.MockIdGenerator;
        this._resolvedRecordIsPersisted = persisted;
    }

    /// <summary>
    /// The resolver decided this registered record already carries its key,
    /// so it is used as-is - no sub-graph generated, no re-insert. The
    /// value-vs-generate call is made here (the real key field is known),
    /// not by the <c>Id</c>-named heuristic in <c>Put(name, record)</c>.
    /// </summary>
    public void AcceptResolvedValue(object record, PropertyInfo primaryField)
    {
        this.resolvedRecord = record;
        this.resolvedBundle = null;
        this.resolvedPrimaryField = primaryField;
        this._resolvedRecordIsPersisted = true;
    }

    /// <summary>The shared record as a single-record sub-bundle. Never null once resolved.</summary>
    public Bundle GetResolvedBundle()
    {
        if (this.resolvedBundle is null && this.resolvedRecord is not null)
        {
            this.resolvedBundle = this.SingleRecordBundle();
        }

        return this.resolvedBundle!;
    }

    private Bundle SingleRecordBundle()
    {
        PropertyInfo idField = this.resolvedPrimaryField ?? this.resolvedRecord!.GetType().GetProperty("Id")!;
        Bundle bundle = new();
        bundle.PutPrimaries(idField, [this.resolvedRecord!], this.resolvedMockIdGenerator);
        return bundle;
    }
}
