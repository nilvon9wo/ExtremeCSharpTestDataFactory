using System.Reflection;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>One up-flowing value still to resolve: records[RecordIndex].Field will be filled by Strategy during the DEFERRED flush, once the whole forest exists.</summary>
public sealed class PendingDeferredValue(int recordIndex, PropertyInfo field, IDeferredExpression strategy)
{
    public int RecordIndex { get; } = recordIndex;

    public PropertyInfo Field { get; } = field;

    public IDeferredExpression Strategy { get; } = strategy;
}
