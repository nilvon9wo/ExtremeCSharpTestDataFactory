namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate{TRecord}"/> satisfied when a record's field
/// orders strictly after a fixed value. A null record, null field value, or
/// null comparison value is never greater.
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldGreaterThanPredicate<TRecord, TValue> : FieldPredicateBase<TRecord, TValue>
    where TRecord : class
{
    private readonly TValue? comparisonValue;

    private FieldGreaterThanPredicate(Func<TRecord, TValue> field, TValue? comparisonValue) : base(field)
    {
        this.comparisonValue = comparisonValue;
    }

    public static FieldGreaterThanPredicate<TRecord, TValue> Of(Func<TRecord, TValue> field, TValue? comparisonValue) =>
        new(field, comparisonValue);

    public override bool IsSatisfiedBy(TRecord? record) =>
        IsGreaterThan(this.ActualValue(record), this.comparisonValue);

    // Comparer<TValue>.Default, not an IComparable<TValue> constraint: a
    // nullable value type (e.g. int?) can never satisfy that constraint, and
    // our demo/record fields are routinely nullable. The null guard below
    // keeps "either side absent" behaving as "never greater", same as Apex's
    // original; Comparer<T>.Default's own null-handling never runs.
    private static bool IsGreaterThan(TValue? actual, TValue? comparisonValue) =>
        actual is not null
        && comparisonValue is not null
        && Comparer<TValue>.Default.Compare(actual, comparisonValue) > 0;
}
