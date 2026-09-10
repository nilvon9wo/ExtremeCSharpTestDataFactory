using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied when a record's field orders
/// strictly after a fixed value - numbers numerically, dates/times
/// chronologically, everything else lexicographically (see
/// <see cref="ValueComparison"/>). A null record, null field value, or null
/// comparison value is never greater.
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldGreaterThanPredicate : IRecordPredicate
{
    private readonly PropertyInfo _field;
    private readonly object? _comparisonValue;

    private FieldGreaterThanPredicate(PropertyInfo field, object? comparisonValue)
    {
        this._field = field;
        this._comparisonValue = comparisonValue;
    }

    public static FieldGreaterThanPredicate Of(PropertyInfo field, object? comparisonValue) =>
        new(field, comparisonValue);

    public bool IsSatisfiedBy(object? record) =>
        ValueComparison.FieldToValue(record, this._field, this._comparisonValue) is > 0;
}