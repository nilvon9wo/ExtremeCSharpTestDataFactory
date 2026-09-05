using System.Linq.Expressions;
using System.Reflection;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Values;

/// <summary>
/// A context-aware value that copies a field from a generated ancestor.
///
/// Single hop:
/// <code>
/// .PutRequired(x => x.AccountId, new DefaultRelationship(new Account()))
/// .Put(x => x.Department, CopyFromAncestorExpression.From&lt;Contact, Account&gt;(x => x.AccountId, x => x.Site))
/// </code>
///
/// Multiple hops - a path of relationship fields ending in the field to read
/// - are supported via the list constructor (each hop still named with
/// <see cref="Field.Of{TRecord}(Expression{Func{TRecord,object}})"/>, since
/// each hop is a different record type). Returns null if any hop of the
/// relationship was not generated (e.g. an optional one skipped by the
/// current inclusivity).
/// </summary>
public sealed class CopyFromAncestorExpression : IContextAwareExpression
{
    // path = [hop1, hop2, ..., hopK, sourceField] - K >= 1 relationship hops then the field to read.
    private readonly List<PropertyInfo> path;

    public CopyFromAncestorExpression(PropertyInfo relationshipField, PropertyInfo sourceField)
        : this([relationshipField, sourceField])
    {
    }

    /// <summary>CopyFromAncestorExpression(relationshipField, sourceField), naming both fields by lambda.</summary>
    public static CopyFromAncestorExpression From<TRelationship, TTarget>(
        Expression<Func<TRelationship, object?>> relationshipField, Expression<Func<TTarget, object?>> sourceField) =>
        new(Field.Of(relationshipField), Field.Of(sourceField));

    public CopyFromAncestorExpression(List<PropertyInfo>? pathEndingInSourceField)
    {
        if (pathEndingInSourceField is not { Count: >= 2 })
        {
            throw new XftyConfigurationException(
                "CopyFromAncestorExpression needs a path of at least one relationship field then the field to read.");
        }

        if (pathEndingInSourceField.Any(step => step is null))
        {
            throw new XftyConfigurationException("CopyFromAncestorExpression path steps cannot be null.");
        }

        this.path = pathEndingInSourceField;
    }

    public object? Get(GenerationContext context) =>
        context.BundleSoFar?.GetValue(this.path, context.RowIndex);
}
