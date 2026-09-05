using System.Linq.Expressions;
using System.Reflection;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Values;

/// <summary>
/// An up-flowing value: a field on a record copied from a generated
/// **child** - the record that references it through childLookupField.
///
/// Needs the DEFERRED insert mode - the child must exist before it can be
/// read - and resolves when the deferred flush runs (not yet ported, see
/// csharp-port-idea.md). With more than one matching child the **first**
/// generated child is read; with none, the value is null.
/// </summary>
public sealed class CopyFromDescendantExpression : IDeferredExpression
{
    private readonly PropertyInfo childLookupField;
    private readonly PropertyInfo sourceField;

    public CopyFromDescendantExpression(PropertyInfo childLookupField, PropertyInfo sourceField)
    {
        this.childLookupField = childLookupField ?? throw new XftyConfigurationException(
            "CopyFromDescendantExpression needs the child lookup field and the field to read from the child.");
        this.sourceField = sourceField ?? throw new XftyConfigurationException(
            "CopyFromDescendantExpression needs the child lookup field and the field to read from the child.");
    }

    /// <summary>CopyFromDescendantExpression(childLookupField, sourceField), naming both fields by lambda.</summary>
    public static CopyFromDescendantExpression From<TChild>(
        Expression<Func<TChild, object?>> childLookupField, Expression<Func<TChild, object?>> sourceField) =>
        new(Field.Of(childLookupField), Field.Of(sourceField));

    public object? Get(DeferredGraph graph, int recordIndex)
    {
        List<object> children = graph.ChildrenOf(recordIndex, this.childLookupField);
        return children.Count == 0
            ? null
            : this.sourceField.GetValue(children[0]);
    }
}
