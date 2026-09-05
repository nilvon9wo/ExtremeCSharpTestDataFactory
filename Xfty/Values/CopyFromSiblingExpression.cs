using System.Linq.Expressions;
using System.Reflection;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Values;

/// <summary>
/// A context-aware value that copies another field from the *same* record.
///
/// <code>
/// .Put(x => x.Name, "Acme")
/// .Put(x => x.Industry, CopyFromSiblingExpression.From&lt;Account&gt;(x => x.Name))
/// </code>
///
/// The sibling must be resolvable when this runs: a plain value (always), or
/// another context-aware value that was put(...) *before* this one. Reading a
/// context-aware sibling that was put(...) *after* this one (or a circular
/// pair) throws loudly from <see cref="GenerationContext.SiblingValue"/> -
/// it is never a silent null.
/// </summary>
public sealed class CopyFromSiblingExpression(PropertyInfo sourceField) : IContextAwareExpression
{
    private readonly PropertyInfo sourceField = sourceField ?? throw new XftyConfigurationException("CopyFromSiblingExpression needs a source field.");

    /// <summary>CopyFromSiblingExpression(field), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public static CopyFromSiblingExpression From<TRecord>(Expression<Func<TRecord, object?>> sourceField) =>
        new(Field.Of(sourceField));

    public object? Get(GenerationContext context) =>
        context.SiblingValue(this.sourceField);
}
