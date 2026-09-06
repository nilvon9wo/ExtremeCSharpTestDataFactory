using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// The narrowest scope of a generation run: the single value field whose
/// context-aware expression is running right now, plus the set of
/// context-aware value fields on the same record that have *not* been
/// generated yet.
///
/// <see cref="PendingContextAwareValues"/> is what lets the engine tell "this
/// sibling has not been computed yet" apart from "this sibling was computed
/// and the answer is null": a field still in the set has no value because its
/// strategy has not run; a field absent from the set holds its real, final
/// value (possibly null). <see cref="GenerationContext.SiblingValue"/> refuses
/// a read of a still-pending field loudly rather than handing back a
/// misleading null.
/// </summary>
public sealed class ValueFieldPass(PropertyInfo fieldBeingBuilt, IReadOnlyCollection<PropertyInfo> pendingContextAwareValues)
{
    public PropertyInfo FieldBeingBuilt { get; } = fieldBeingBuilt;

    public IReadOnlyCollection<PropertyInfo> PendingContextAwareValues { get; } = pendingContextAwareValues;
}
