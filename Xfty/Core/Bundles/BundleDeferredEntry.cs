using System.Reflection;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.Bundles;

/// <summary>One primary row's field, still to be filled by an up-flow strategy during the DEFERRED flush.</summary>
public sealed class BundleDeferredEntry(int primaryRow, PropertyInfo field, IDeferred strategy)
{
    public int PrimaryRow { get; } = primaryRow;

    public PropertyInfo Field { get; } = field;

    public IDeferred Strategy { get; } = strategy;
}