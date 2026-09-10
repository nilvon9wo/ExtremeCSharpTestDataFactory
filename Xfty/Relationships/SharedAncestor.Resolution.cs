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
        if (this._resolvedRecord is null)
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
        this._resolvedPrimaryField ??=
            this._resolvedBundle?.PrimaryTargetField
            ?? this.PrimaryFieldFromProvider(lookup);

        // Only the PutAsValue path (no bundle) guessed persistence from an "Id"-named property; correct it now.
        bool cameFromPutAsValue = this._resolvedBundle is null && this._resolvedRecord is not null;
        if (cameFromPutAsValue && this._resolvedPrimaryField is not null)
        {
            this.IsResolvedRecordPersisted = this._resolvedPrimaryField.GetValue(this._resolvedRecord) is not null;
        }
    }

    /// <summary>
    /// The Provider's primary-key field, when the source has a template or variant key to resolve one from; null for a
    /// bare PutAsValue registration.
    /// </summary>
    private PropertyInfo? PrimaryFieldFromProvider(IProviderLookup lookup) =>
        this._source is { } theSource && theSource.CanResolvePrimaryField
            ? theSource.PrimaryField(lookup)
            : null;

    public SharedAncestorProvider Source() =>
        this._source ?? throw new XftyConfigurationException(
            $"Shared ancestor \"{this.SharedName}\" was never registered - call "
            + $"SharedAncestor.Put(\"{this.SharedName}\", template / key)."
        );

    /// <summary>The resolver hands back the generated record and its graph.</summary>
    public void AcceptResolved(object record, Bundle bundle, bool persisted)
    {
        this._resolvedRecord = record;
        this._resolvedBundle = bundle;
        this._resolvedPrimaryField = bundle.PrimaryTargetField;
        this._resolvedMockIdGenerator = bundle.MockIdGenerator;
        this.IsResolvedRecordPersisted = persisted;
    }

    /// <summary>
    /// The resolver decided this registered record already carries its key,
    /// so it is used as-is - no sub-graph generated, no re-insert. The
    /// value-vs-generate call is made here (the real key field is known),
    /// not by the <c>Id</c>-named heuristic in <c>Put(name, record)</c>.
    /// </summary>
    public void AcceptResolvedValue(object record, PropertyInfo primaryField)
    {
        this._resolvedRecord = record;
        this._resolvedBundle = null;
        this._resolvedPrimaryField = primaryField;
        this.IsResolvedRecordPersisted = true;
    }

    /// <summary>The shared record as a single-record sub-bundle. Never null once resolved.</summary>
    public Bundle GetResolvedBundle()
    {
        if (this._resolvedBundle is null && this._resolvedRecord is not null)
        {
            this._resolvedBundle = this.SingleRecordBundle();
        }

        return this._resolvedBundle!;
    }

    private Bundle SingleRecordBundle()
    {
        PropertyInfo idField =
            this._resolvedPrimaryField ?? this._resolvedRecord!.GetType().GetProperty(ConventionalIdFieldName)!;
        Bundle bundle = new();
        bundle.PutPrimaries(idField, [this._resolvedRecord!], this._resolvedMockIdGenerator);
        return bundle;
    }
}