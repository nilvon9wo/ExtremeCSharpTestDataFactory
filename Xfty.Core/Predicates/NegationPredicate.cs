namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate{TRecord}"/> satisfied exactly when the
/// predicate it wraps is not (logical NOT).
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class NegationPredicate<TRecord> : IRecordPredicate<TRecord>
{
    private readonly IRecordPredicate<TRecord> negated;

    private NegationPredicate(IRecordPredicate<TRecord> negated)
    {
        this.negated = negated;
    }

    public static NegationPredicate<TRecord> Of(IRecordPredicate<TRecord>? predicate)
    {
        if (predicate is null)
        {
            throw new XftyConfigurationException("A predicate to negate is required.");
        }
        return new NegationPredicate<TRecord>(predicate);
    }

    public bool IsSatisfiedBy(TRecord? record) =>
        !this.negated.IsSatisfiedBy(record);
}
