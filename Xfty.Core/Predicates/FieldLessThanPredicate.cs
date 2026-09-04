namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate{TRecord}"/> satisfied when a record's field
/// orders strictly before a fixed value. A null record, null field value, or
/// null comparison value is never less.
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldLessThanPredicate<TRecord, TValue> : FieldPredicateBase<TRecord, TValue>
    where TRecord : class
{
    private readonly TValue? comparisonValue;

    private FieldLessThanPredicate(Func<TRecord, TValue> field, TValue? comparisonValue) : base(field)
    {
        this.comparisonValue = comparisonValue;
    }

    public static FieldLessThanPredicate<TRecord, TValue> Of(Func<TRecord, TValue> field, TValue? comparisonValue) =>
        new(field, comparisonValue);

    public override bool IsSatisfiedBy(TRecord? record) =>
        IsLessThan(this.ActualValue(record), this.comparisonValue);

    // See FieldGreaterThanPredicate for why this is Comparer<TValue>.Default
    // rather than an IComparable<TValue> constraint.
    private static bool IsLessThan(TValue? actual, TValue? comparisonValue) =>
        actual is not null
        && comparisonValue is not null
        && Comparer<TValue>.Default.Compare(actual, comparisonValue) < 0;
}
