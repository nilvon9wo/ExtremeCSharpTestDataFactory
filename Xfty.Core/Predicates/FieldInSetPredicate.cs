namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate{TRecord}"/> satisfied when a field's value is
/// one of a fixed set. A null set is treated as empty (nothing matches).
///
/// Obtain one through <see cref="Of"/> or the <see cref="FieldPredicateFactory"/>
/// facade.
/// </summary>
public sealed class FieldInSetPredicate<TRecord, TValue> : FieldPredicateBase<TRecord, TValue>
    where TRecord : class
{
    private readonly HashSet<TValue?> acceptedValues;

    private FieldInSetPredicate(Func<TRecord, TValue> field, IEnumerable<TValue?>? acceptedValues) : base(field)
    {
        this.acceptedValues = acceptedValues is null
            ? new HashSet<TValue?>()
            : new HashSet<TValue?>(acceptedValues);
    }

    public static FieldInSetPredicate<TRecord, TValue> Of(
        Func<TRecord, TValue> field,
        IEnumerable<TValue?>? acceptedValues) =>
        new(field, acceptedValues);

    public override bool IsSatisfiedBy(TRecord? record) =>
        this.acceptedValues.Contains(this.ActualValue(record));
}
