using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Core;

/// <summary>One primary row's field, still to be filled by an up-flow strategy during the DEFERRED flush.</summary>
public sealed class BundleDeferredEntry
{
    public int PrimaryRow { get; }

    public PropertyInfo Field { get; }

    public IDeferredExpression Strategy { get; }

    public BundleDeferredEntry(int primaryRow, PropertyInfo field, IDeferredExpression strategy)
    {
        this.PrimaryRow = primaryRow;
        this.Field = field;
        this.Strategy = strategy;
    }
}
