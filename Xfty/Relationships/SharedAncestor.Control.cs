using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Relationships;

/// <summary>SharedAncestor - developer control over resolution: Disable, manual-resolution mode, batched ResolveNow by name.</summary>
public sealed partial class SharedAncestor
{
    /// <summary>This shared ancestor is never resolved; any reference to it leaves the child's foreign key null.</summary>
    public static void Disable(string name)
    {
        Get(name).AssertUnresolved("Disable(...)");
        _ = Disabled.TryAdd(name, 0);
    }

    /// <summary>Turn off the pre-phase that auto-resolves every registered shared ancestor.</summary>
    public static void ManualResolutionOnly() => _manualResolution = true;

    public static bool IsManualResolutionOnly() => _manualResolution;

    /// <summary>Resolve a named set of shared ancestors up front, in one depth-batched pass.</summary>
    public static Task ResolveNow(IProviderLookup lookup, InsertMode insertMode, List<string> names)
    {
        SharedAncestorResolver.ApplyLookupDefaults(lookup);
        List<SharedAncestor> toResolve = [.. names.Select(Get).Where(ancestor => ancestor.resolvedRecord is null)];
        return toResolve.Count > 0
            ? new SharedAncestorResolver(lookup, insertMode).Resolve(toResolve)
            : Task.CompletedTask;
    }

    /// <summary>Every registered ancestor not yet resolved.</summary>
    public static List<SharedAncestor> ConfiguredUnresolved() => [.. ByName.Values.Where(IsUnresolvedAndEnabled)];

    private static bool IsUnresolvedAndEnabled(SharedAncestor ancestor) =>
        ancestor.source is not null && ancestor.resolvedRecord is null && !Disabled.ContainsKey(ancestor._name);

    private bool IsUnregistered() => this.source is null && this.resolvedRecord is null;

    public void AssertUnresolved(string call)
    {
        if (this.resolvedRecord is not null)
        {
            throw new XftyConfigurationException($"Shared ancestor \"{this._name}\" is already resolved; {call} would have no effect.");
        }
    }
}
