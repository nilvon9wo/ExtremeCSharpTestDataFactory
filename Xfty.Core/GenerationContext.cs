using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The state a context-aware value expression reads while it runs.
///
/// This is a **deliberately partial** port of Apex's <c>XFTY_GenerationContext</c>
/// - only <see cref="RecordBeingBuilt"/> and <see cref="ValueFieldPass"/> exist
/// so far, enough for <c>CopyFromSiblingExpression</c> to work end-to-end. The
/// Apex original also carries the Provider Lookup, insert mode/inclusivity,
/// the in-progress bundle (for <c>CopyFromAncestorExpression</c>), forced-
/// relationship paths, path-value overrides, the ancestor cycle guard, and a
/// batched-insert flag - those get added here once the types they depend on
/// (the bundle representation, relationships/, lookup/) are ported. See
/// csharp-port-idea.md.
/// </summary>
public sealed class GenerationContext
{
    public object? RecordBeingBuilt { get; }

    public ValueFieldPass? ValueFieldPass { get; }

    public GenerationContext(object? recordBeingBuilt, ValueFieldPass? valueFieldPass)
    {
        this.RecordBeingBuilt = recordBeingBuilt;
        this.ValueFieldPass = valueFieldPass;
    }

    /// <summary>
    /// The final value of a sibling field on <see cref="RecordBeingBuilt"/>,
    /// for a context-aware expression. A returned null means the sibling was
    /// genuinely generated to null.
    ///
    /// Throws when <paramref name="siblingField"/> is itself a context-aware
    /// value that has not been generated yet - the one case where put(...)
    /// order matters - so the mistake surfaces loudly instead of as a silent
    /// wrong null.
    /// </summary>
    public object? SiblingValue(PropertyInfo siblingField) =>
        this.ValueFieldPass switch
        {
            null => throw new XftyConfigurationException(
                $"SiblingValue({siblingField.Name}) can only be read while a context-aware value is being generated."),
            { } pass when pass.PendingContextAwareValues.Contains(siblingField) => throw new XftyConfigurationException(
                $"The context-aware value for {pass.FieldBeingBuilt.Name} reads sibling field {siblingField.Name}, "
                + "which is itself a context-aware value that has not been generated yet. Context-aware values are "
                + $"generated in the order they are put, so .Put({siblingField.Name}, ...) must come before "
                + $".Put({pass.FieldBeingBuilt.Name}, ...)."),
            _ => this.RecordBeingBuilt is null
                ? null
                : siblingField.GetValue(this.RecordBeingBuilt),
        };
}
