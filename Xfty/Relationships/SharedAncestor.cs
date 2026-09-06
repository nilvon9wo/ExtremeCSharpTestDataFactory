using System.Collections.Concurrent;
using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>
/// One record shared by every relationship that references it - the same
/// instance and Id everywhere, generated at most once per test method.
///
/// Put(name, ...) registers and returns a <see cref="SharedAncestorProvider"/>;
/// Get(name) only retrieves the token to hand to PutRequired/PutOptional, and
/// the handle for ResolveNow/GetId.
///
/// Flyweight - state is static, and safe under concurrent access:
/// <see cref="ByName"/>/<see cref="Disabled"/> are concurrent collections,
/// <see cref="_manualResolution"/> is <c>volatile</c>, and the actual
/// resolve-and-mutate work is serialized through <see cref="SharedAncestorResolver"/>'s
/// own lock (not this class's concern - every entry point that can trigger
/// resolution ends up calling into that resolver). This matters because
/// xUnit's *default* behaviour (unlike this port's own test suite, which
/// opts out) is to run different test classes in parallel - a real,
/// previously-uncaught crash risk, not a theoretical one; see
/// reference/known-issues.md. <see cref="SharedAncestorResolver"/> resolves
/// every registered ancestor before the first Supply*() call. Split across
/// several files by concern: this file is identity and the flyweight
/// registry; SharedAncestor.Registration.cs is Put*(...); SharedAncestor.
/// Control.cs is Disable/ManualResolutionOnly/ResolveNow(names)/...;
/// SharedAncestor.Resolution.cs is instance resolution; SharedAncestor.
/// Relationship.cs is the IDefaultRelationship/ISharedRelationship surface.
/// </summary>
public sealed partial class SharedAncestor : ISharedRelationship
{
    private static readonly ConcurrentDictionary<string, SharedAncestor> ByName = new();
    private static readonly ConcurrentDictionary<string, byte> Disabled = new();
    private static volatile bool _manualResolution;

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
        return ByName.GetOrAdd(name, static key => new SharedAncestor(key));
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
        if (Disabled.ContainsKey(name))
        {
            throw new XftyConfigurationException($"Shared ancestor \"{name}\" is disabled.");
        }
    }

    private static XftyConfigurationException NotYetResolved(string name) =>
        new(
            $"Shared ancestor \"{name}\" is not resolved yet. Reference it in a Supply*() call first, or call "
            + $"SharedAncestor.Get(\"{name}\").ResolveNow(lookup, mode).");

    private static object? IdOf(object? record) => record?.GetType().GetProperty("Id")?.GetValue(record);

    /// <summary>
    /// Clears every registered/disabled shared ancestor and the manual-
    /// resolution flag - for test isolation only. .NET statics have no
    /// per-test-method lifecycle the way Apex's do, so nothing in XFTY
    /// calls this automatically; call it yourself from your own test
    /// suite's per-test setup (a base test class's constructor, or an
    /// xUnit fixture's Dispose) if you rely on SharedAncestor across many
    /// tests. Also the only safe way to test
    /// <see cref="ManualResolutionOnly()"/>, which otherwise has no
    /// unsetter at all - see reference/salesforce-considerations.md.
    /// </summary>
    public static void ResetAllForTesting()
    {
        ByName.Clear();
        Disabled.Clear();
        _manualResolution = false;
    }
}
