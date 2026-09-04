namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate{TRecord}"/> satisfied when a record's field
/// equals a fixed value - null included, so <c>EqualTo(field, null)</c> is an
/// "is null" check. Wrap it in <see cref="NegationPredicate{TRecord}"/> for
/// "not equal" / "is not null".
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldEqualToPredicate<TRecord, TValue> : FieldPredicateBase<TRecord, TValue>
    where TRecord : class
{
    private readonly TValue? comparisonValue;

    private FieldEqualToPredicate(Func<TRecord, TValue> field, TValue? comparisonValue) : base(field)
    {
        this.comparisonValue = comparisonValue;
    }

    public static FieldEqualToPredicate<TRecord, TValue> Of(Func<TRecord, TValue> field, TValue? comparisonValue) =>
        new(field, comparisonValue);

    public override bool IsSatisfiedBy(TRecord? record) =>
        EqualityComparer<TValue?>.Default.Equals(this.ActualValue(record), this.comparisonValue);
}
