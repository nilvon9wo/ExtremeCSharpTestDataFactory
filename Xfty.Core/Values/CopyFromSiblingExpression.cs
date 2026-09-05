using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core.Values;

/// <summary>
/// A context-aware value that copies another field from the *same* record.
///
/// <code>
/// .Put(Field.Of&lt;Account&gt;(nameof(Account.Name)), new LiteralExpression("Acme"))
/// .Put(Field.Of&lt;Account&gt;(nameof(Account.Industry)), new CopyFromSiblingExpression(Field.Of&lt;Account&gt;(nameof(Account.Name))))
/// </code>
///
/// The sibling must be resolvable when this runs: a plain value (always), or
/// another context-aware value that was put(...) *before* this one. Reading a
/// context-aware sibling that was put(...) *after* this one (or a circular
/// pair) throws loudly from <see cref="GenerationContext.SiblingValue"/> -
/// it is never a silent null.
/// </summary>
public sealed class CopyFromSiblingExpression : IContextAwareExpression
{
    private readonly PropertyInfo sourceField;

    public CopyFromSiblingExpression(PropertyInfo sourceField) =>
        this.sourceField = sourceField ?? throw new XftyConfigurationException("CopyFromSiblingExpression needs a source field.");

    public object? Get(GenerationContext context) =>
        context.SiblingValue(this.sourceField);
}
