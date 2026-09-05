using Net.Nowhereatall.Xfty.Core;

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
/// every registered ancestor before the first Supply*() call. Split across
/// several files by concern: this file is identity and the flyweight
/// registry; SharedAncestor.Registration.cs is Put*(...); SharedAncestor.
/// Control.cs is Disable/ManualResolutionOnly/ResolveNow(names)/...;
/// SharedAncestor.Resolution.cs is instance resolution; SharedAncestor.
/// Relationship.cs is the IDefaultRelationship/ISharedRelationship surface.
/// </summary>
public sealed partial class SharedAncestor : ISharedRelationship
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

    /// <summary>The interned instance for name - the token for PutRequired(field, ...). Creates it on first use.</summary>
    public static SharedAncestor Get(string name)
    {
        AssertNameGiven(name);
        if (!ByName.TryGetValue(name, out SharedAncestor? existing))
        {
            existing = new SharedAncestor(name);
            ByName[name] = existing;
        }

        return existing;
    }

    private static void AssertNameGiven(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new XftyConfigurationException("A shared ancestor needs a non-blank name.");
        }
    }

    public static object GetId(string name)
    {
        AssertNotDisabled(name);
        SharedAncestor ancestor = Get(name);
        return ancestor.resolvedRecord is null ? throw NotYetResolved(name) : IdOf(ancestor.resolvedRecord)!;
    }

    private static void AssertNotDisabled(string name)
    {
        if (Disabled.Contains(name))
        {
            throw new XftyConfigurationException($"Shared ancestor \"{name}\" is disabled.");
        }
    }

    private static XftyConfigurationException NotYetResolved(string name) =>
        new(
            $"Shared ancestor \"{name}\" is not resolved yet. Reference it in a Supply*() call first, or call "
            + $"SharedAncestor.Get(\"{name}\").ResolveNow(lookup, mode).");

    private static object? IdOf(object? record) => record?.GetType().GetProperty("Id")?.GetValue(record);
}
