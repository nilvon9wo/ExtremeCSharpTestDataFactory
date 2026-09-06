using System.Reflection;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>
/// The up-flowing values a bundle's primaries still owe - one (row, field,
/// strategy) per primary row per deferred field. Collected while the bundle
/// is generated and drained during the DEFERRED flush, once the whole forest
/// exists and the descendants those values read from are real.
/// </summary>
public sealed class DeferredValueQueue
{
    private readonly List<BundleDeferredEntry> entries = [];

    /// <summary>Queue each byField entry for every one of rowCount primary rows.</summary>
    public void AddForEachRow(int rowCount, Dictionary<PropertyInfo, IDeferredExpression> byField) =>
        Enumerable.Range(0, rowCount)
            .SelectMany(row => byField.Select(pair => new BundleDeferredEntry(row, pair.Key, pair.Value)))
            .ToList()
            .ForEach(this.entries.Add);

    public List<BundleDeferredEntry> Entries() => this.entries;
}
