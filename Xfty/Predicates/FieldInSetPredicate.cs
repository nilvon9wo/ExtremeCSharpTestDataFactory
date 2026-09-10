using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied when a field's value is one of
/// a fixed set. A null set is treated as empty (nothing matches).
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldInSetPredicate : IRecordPredicate
{
    private readonly PropertyInfo _field;
    private readonly HashSet<object?> _acceptedValues;

    private FieldInSetPredicate(PropertyInfo field, IEnumerable<object?>? acceptedValues)
    {
        this._field = field;
        this._acceptedValues = acceptedValues is null
            ? []
            : [.. acceptedValues];
    }

    public static FieldInSetPredicate Of(PropertyInfo field, IEnumerable<object?>? acceptedValues) =>
        new(field, acceptedValues);

    public bool IsSatisfiedBy(object? record)
    {
        object? actual = record is null
            ? null
            : this._field.GetValue(record);
        return this._acceptedValues.Contains(actual);
    }
}