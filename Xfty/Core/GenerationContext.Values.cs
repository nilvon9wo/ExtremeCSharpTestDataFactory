using System.Reflection;
using Net.Nowhereatall.Xfty.Engine;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>GenerationContext - reading a sibling field's value during the context-aware value pass.</summary>
public sealed partial class GenerationContext
{
    /// <summary>
    /// The final value of a sibling field on RecordBeingBuilt, for a
    /// context-aware expression. A returned null means the sibling was
    /// genuinely generated to null.
    ///
    /// Throws when siblingField is itself a context-aware value that has not
    /// been generated yet - the one case where Put(...) order matters - so
    /// the mistake surfaces loudly instead of as a silent wrong null.
    /// </summary>
    public object? SiblingValue(PropertyInfo siblingField) =>
        this.ValueFieldPass switch
        {
            null => throw NotDuringContextAwarePass(siblingField),
            { } pass when pass.PendingContextAwareValues.Contains(siblingField) => throw SiblingNotYetGenerated(pass, siblingField),
            _ => this.RecordBeingBuilt is null ? null : siblingField.GetValue(this.RecordBeingBuilt),
        };

    private static XftyConfigurationException NotDuringContextAwarePass(PropertyInfo siblingField) =>
        new($"SiblingValue({siblingField.Name}) can only be read while a context-aware value is being generated.");

    private static XftyConfigurationException SiblingNotYetGenerated(ValueFieldPass pass, PropertyInfo siblingField) =>
        new(
            $"The context-aware value for {pass.FieldBeingBuilt.Name} reads sibling field {siblingField.Name}, "
            + "which is itself a context-aware value that has not been generated yet. Context-aware values are "
            + $"generated in the order they are put, so .Put({siblingField.Name}, ...) must come before "
            + $".Put({pass.FieldBeingBuilt.Name}, ...).");
}
