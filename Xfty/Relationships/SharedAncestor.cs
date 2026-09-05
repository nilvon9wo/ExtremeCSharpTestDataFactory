using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Relationships;

/// <summary>
/// One record shared by every relationship that references it - the same
/// instance and Id everywhere, generated at most once per test method.
///
/// Put(name, ...) registers and returns a <see cref="SharedAncestorProvider"/>;
/// Get(name) only retrieves the token to hand to PutRequired/PutOptional, and
/// the handle for ResolveNow/GetId.
///
/// Flyweight - state is static. <see cref="SharedAncestorResolver"/> resolves
/// every registered ancestor before the first Supply*() call.
/// </summary>
public sealed class SharedAncestor : ISharedRelationship
{
    private static readonly Dictionary<string, SharedAncestor> ByName = [];
    private static readonly HashSet<string> Disabled = [];
    private static bool manualResolution;

    private string _name { get; }

    private SharedAncestorProvider? source;
    private object? resolvedRecord;
    private Bundle? resolvedBundle;
    private bool _resolvedRecordIsPersisted { get; set; }

    private SharedAncestor(string name) => this._name = name;

    // Retrieval ---------------------------------------------------------

    /// <summary>The interned instance for name - the token for PutRequired(field, ...). Creates it on first use.</summary>
    public static SharedAncestor Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new XftyConfigurationException("A shared ancestor needs a non-blank name.");
        }

        if (!ByName.TryGetValue(name, out SharedAncestor? existing))
        {
            existing = new SharedAncestor(name);
            ByName[name] = existing;
        }

        return existing;
    }

    public static object GetId(string name)
    {
        if (Disabled.Contains(name))
        {
            throw new XftyConfigurationException($"Shared ancestor \"{name}\" is disabled.");
        }

        SharedAncestor ancestor = Get(name);
        return ancestor.resolvedRecord is null
            ? throw new XftyConfigurationException(
                $"Shared ancestor \"{name}\" is not resolved yet. Reference it in a Supply*() call first, or call "
                + $"SharedAncestor.Get(\"{name}\").ResolveNow(lookup, mode).")
            : IdOf(ancestor.resolvedRecord)!;
    }

    // Registration ----------------------------------------------------

    /// <summary>Register record. Disambiguates by Id: with one, a fixed value; without, an override template.</summary>
    public static SharedAncestorProvider Put(string name, object? record) =>
        IdOf(record) is not null
            ? PutAsValue(name, record!)
            : PutAsTemplate(name, record);

    /// <summary>Register an override template; the shared record is generated from it in the pre-phase.</summary>
    public static SharedAncestorProvider PutAsTemplate(string name, object? template) => Get(name).Provider().WithTemplate(template);

    /// <summary>Register a record the test built itself; used as-is.</summary>
    public static SharedAncestorProvider PutAsValue(string name, object record)
    {
        SharedAncestor ancestor = Get(name);
        ancestor.resolvedRecord = record;
        ancestor.resolvedBundle = null;
        ancestor._resolvedRecordIsPersisted = IdOf(record) is not null;
        return ancestor.Provider();
    }

    /// <summary>Register just the Provider variant that generates the shared record.</summary>
    public static SharedAncestorProvider Put(string name, ILookupKey variantKey) => Get(name).Provider().FromVariant(variantKey);

    /// <summary>Put(name, record) (same Id-disambiguation), applied only if name is not registered yet.</summary>
    public static SharedAncestorProvider PutIfAbsent(string name, object? record)
    {
        SharedAncestor ancestor = Get(name);
        return ancestor.IsUnregistered() ? Put(name, record) : ancestor.Provider();
    }

    /// <summary>As PutIfAbsent(string,object), pinning the variant instead of a template.</summary>
    public static SharedAncestorProvider PutIfAbsent(string name, ILookupKey variantKey)
    {
        SharedAncestor ancestor = Get(name);
        return ancestor.IsUnregistered() ? Put(name, variantKey) : ancestor.Provider();
    }

    // Developer control over resolution -----------------------------

    /// <summary>This shared ancestor is never resolved; any reference to it leaves the child's foreign key null.</summary>
    public static void Disable(string name)
    {
        Get(name).AssertUnresolved("Disable(...)");
        _ = Disabled.Add(name);
    }

    /// <summary>Turn off the pre-phase that auto-resolves every registered shared ancestor.</summary>
    public static void ManualResolutionOnly() => manualResolution = true;

    public static bool IsManualResolutionOnly() => manualResolution;

    /// <summary>Resolve a named set of shared ancestors up front, in one depth-batched pass.</summary>
    public static void ResolveNow(IProviderLookup lookup, InsertMode insertMode, List<string> names)
    {
        SharedAncestorResolver.ApplyLookupDefaults(lookup);
        List<SharedAncestor> toResolve = names.Select(Get).Where(ancestor => ancestor.resolvedRecord is null).ToList();
        if (toResolve.Count > 0)
        {
            new SharedAncestorResolver(lookup, insertMode).Resolve(toResolve);
        }
    }

    /// <summary>Every registered ancestor not yet resolved.</summary>
    public static List<SharedAncestor> ConfiguredUnresolved() =>
        ByName.Values
            .Where(ancestor => ancestor.source is not null && ancestor.resolvedRecord is null && !Disabled.Contains(ancestor._name))
            .ToList();

    private bool IsUnregistered() => this.source is null && this.resolvedRecord is null;

    private SharedAncestorProvider Provider() => this.source ??= new SharedAncestorProvider(this);

    public void AssertUnresolved(string call)
    {
        if (this.resolvedRecord is not null)
        {
            throw new XftyConfigurationException($"Shared ancestor \"{this._name}\" is already resolved; {call} would have no effect.");
        }
    }

    // Resolution ------------------------------------------------------

    /// <summary>Resolve now - e.g. to read GetId(name) before any Supply*() call.</summary>
    public SharedAncestor ResolveNow(IProviderLookup lookup, InsertMode insertMode)
    {
        if (this.resolvedRecord is null)
        {
            SharedAncestorResolver.ApplyLookupDefaults(lookup);
            new SharedAncestorResolver(lookup, insertMode).Resolve([this]);
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

    // IDefaultRelationship --------------------------------------------

    public object? OverrideTemplate => this.source?.OverrideTemplate();

    public PropertyInfo? RelatedField => this.source?.RelatedField();

    public ILookupKey? ResolveLookupKey(IProviderLookup providerLookup) => this.Source().LookupKey(providerLookup);

    // ISharedRelationship -------------------------------------------

    public string SharedName => this._name;

    public bool IsResolved => this.resolvedRecord is not null;

    public bool IsResolvedRecordPersisted => this._resolvedRecordIsPersisted;

    public object? ResolveSharedRecord(GenerationContext context)
    {
        if (Disabled.Contains(this._name))
        {
            return null;
        }

        if (this.resolvedRecord is not null)
        {
            return this.resolvedRecord;
        }

        if (manualResolution)
        {
            return this.ResolveUnderManualMode(context);
        }

        SharedAncestorResolver.ResolveAllConfigured(context.ProviderLookup, context.InsertMode);
        return this.ResolveNow(context.ProviderLookup, context.InsertMode).resolvedRecord;
    }

    private object? ResolveUnderManualMode(GenerationContext context) =>
        this.Source().IsLightweight(context.ProviderLookup)
            ? this.ResolveNow(context.ProviderLookup, context.InsertMode).resolvedRecord
            : throw new XftyConfigurationException(
                $"Shared ancestor \"{this._name}\" has a sub-graph of its own and auto-resolution is off (manual "
                + $"resolution only). Resolve it up front: SharedAncestor.Get(\"{this._name}\").ResolveNow(lookup, mode), "
                + "or SharedAncestor.ResolveNow(lookup, mode, names).");

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

    private static object? IdOf(object? record) => record?.GetType().GetProperty("Id")?.GetValue(record);
}
