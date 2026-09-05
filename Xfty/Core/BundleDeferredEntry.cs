using System.Reflection;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>One primary row's field, still to be filled by an up-flow strategy during the DEFERRED flush.</summary>
public sealed class BundleDeferredEntry(int primaryRow, PropertyInfo field, IDeferredExpression strategy)
{
    public int PrimaryRow { get; } = primaryRow;

    public PropertyInfo Field { get; } = field;

    public IDeferredExpression Strategy { get; } = strategy;
}
