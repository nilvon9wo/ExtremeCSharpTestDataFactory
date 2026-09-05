using System.Reflection;

namespace Net.Nowhereatall.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied when a record's field equals a
/// fixed value - null included, so <c>EqualTo(field, null)</c> is an "is
/// null" check. Wrap it in <see cref="NegationPredicate"/> for "not equal" /
/// "is not null".
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldEqualToPredicate : IRecordPredicate
{
    private readonly PropertyInfo field;
    private readonly object? comparisonValue;

    private FieldEqualToPredicate(PropertyInfo field, object? comparisonValue)
    {
        this.field = field;
        this.comparisonValue = comparisonValue;
    }

    public static FieldEqualToPredicate Of(PropertyInfo field, object? comparisonValue) =>
        new(field, comparisonValue);

    public bool IsSatisfiedBy(object? record)
    {
        object? actual = record is null
            ? null
            : this.field.GetValue(record);
        return Equals(actual, this.comparisonValue);
    }
}
