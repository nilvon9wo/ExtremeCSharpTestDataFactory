using System.Reflection;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>One up-flowing value still to resolve: records[RecordIndex].Field will be filled by Strategy during the DEFERRED flush, once the whole forest exists.</summary>
public sealed class PendingDeferredValue
{
    public int RecordIndex { get; }

    public PropertyInfo Field { get; }

    public IDeferredExpression Strategy { get; }

    public PendingDeferredValue(int recordIndex, PropertyInfo field, IDeferredExpression strategy)
    {
        this.RecordIndex = recordIndex;
        this.Field = field;
        this.Strategy = strategy;
    }
}
