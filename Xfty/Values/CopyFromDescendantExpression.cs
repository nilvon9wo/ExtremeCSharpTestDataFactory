using System.Linq.Expressions;
using System.Reflection;

using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;
namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>
/// An up-flowing value: a field copied from a generated **descendant** - the
/// record that references this one through childLookupField, or (via the
/// path-list constructor) a chain of such lookups ending at a grandchild or
/// deeper.
///
/// Single hop:
/// <code>
/// new CopyFromDescendantExpression(Field.Of&lt;Case&gt;(x => x.AccountId), Field.Of&lt;Case&gt;(x => x.Subject))
/// </code>
///
/// Multiple hops - a path of child-lookup fields ending in the field to read
/// - are supported via the list constructor, mirroring
/// <see cref="CopyFromAncestorExpression"/>'s. At every hop, including the
/// last, the **first** matching child is read; with no match at any hop, the
/// value is <c>null</c>.
///
/// Needs the DEFERRED insert mode - a descendant must exist before it can be
/// read - and resolves when the deferred flush runs.
/// </summary>
public sealed class CopyFromDescendantExpression : IDeferredExpression
{
    // path = [hop1, hop2, ..., hopK, sourceField] - K >= 1 child-lookup hops then the field to read.
    private readonly List<PropertyInfo> path;

    public CopyFromDescendantExpression(PropertyInfo childLookupField, PropertyInfo sourceField)
        : this([childLookupField, sourceField])
    {
    }

    /// <summary>CopyFromDescendantExpression(childLookupField, sourceField), naming both fields by lambda.</summary>
    public static CopyFromDescendantExpression From<TChild>(
        Expression<Func<TChild, object?>> childLookupField, Expression<Func<TChild, object?>> sourceField) =>
        new(Field.Of(childLookupField), Field.Of(sourceField));

    public CopyFromDescendantExpression(List<PropertyInfo>? pathEndingInSourceField)
    {
        if (pathEndingInSourceField is not { Count: >= 2 })
        {
            throw new XftyConfigurationException(
                "CopyFromDescendantExpression needs a path of at least one child-lookup field then the field to read.");
        }

        if (pathEndingInSourceField.Any(step => step is null))
        {
            throw new XftyConfigurationException("CopyFromDescendantExpression path steps cannot be null.");
        }

        this.path = pathEndingInSourceField;
    }

    public object? Get(DeferredGraph graph, int recordIndex)
    {
        int? descendantIndex = this.WalkHops(graph, recordIndex, hopNumber: 0);
        return descendantIndex is null
            ? null
            : this.path[^1].GetValue(graph.RecordAt(descendantIndex.Value));
    }

    private int? WalkHops(DeferredGraph graph, int currentIndex, int hopNumber)
    {
        if (hopNumber == this.path.Count - 1)
        {
            return currentIndex;
        }

        List<int> childIndices = graph.ChildIndicesOf(currentIndex, this.path[hopNumber]);
        return childIndices.Count == 0
            ? null
            : this.WalkHops(graph, childIndices[0], hopNumber + 1);
    }
}
