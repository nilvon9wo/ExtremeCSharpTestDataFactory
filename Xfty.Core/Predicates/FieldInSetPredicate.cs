using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied when a field's value is one of
/// a fixed set. A null set is treated as empty (nothing matches).
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldInSetPredicate : IRecordPredicate
{
    private readonly PropertyInfo field;
    private readonly HashSet<object?> acceptedValues;

    private FieldInSetPredicate(PropertyInfo field, IEnumerable<object?>? acceptedValues)
    {
        this.field = field;
        this.acceptedValues = acceptedValues is null
            ? new HashSet<object?>()
            : new HashSet<object?>(acceptedValues);
    }

    public static FieldInSetPredicate Of(PropertyInfo field, IEnumerable<object?>? acceptedValues) =>
        new(field, acceptedValues);

    public bool IsSatisfiedBy(object? record)
    {
        object? actual = record is null
            ? null
            : this.field.GetValue(record);
        return this.acceptedValues.Contains(actual);
    }
}
