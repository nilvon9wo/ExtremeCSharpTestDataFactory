using System.Reflection;

namespace Net.Nowhereatall.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied when a record's field orders
/// strictly before a fixed value - numbers numerically, dates/times
/// chronologically, everything else lexicographically (see
/// <see cref="ValueComparison"/>). A null record, null field value, or null
/// comparison value is never less.
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldLessThanPredicate : IRecordPredicate
{
    private readonly PropertyInfo field;
    private readonly object? comparisonValue;

    private FieldLessThanPredicate(PropertyInfo field, object? comparisonValue)
    {
        this.field = field;
        this.comparisonValue = comparisonValue;
    }

    public static FieldLessThanPredicate Of(PropertyInfo field, object? comparisonValue) =>
        new(field, comparisonValue);

    public bool IsSatisfiedBy(object? record) =>
        ValueComparison.FieldToValue(record, this.field, this.comparisonValue) is < 0;
}
