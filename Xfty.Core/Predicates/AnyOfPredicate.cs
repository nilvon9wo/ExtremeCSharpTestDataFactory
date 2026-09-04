namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate{TRecord}"/> satisfied when at least one
/// member predicate is (logical OR). An empty member list is never satisfied.
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class AnyOfPredicate<TRecord> : IRecordPredicate<TRecord>
{
    private readonly IReadOnlyList<IRecordPredicate<TRecord>> members;

    private AnyOfPredicate(IReadOnlyList<IRecordPredicate<TRecord>> members)
    {
        this.members = members;
    }

    public static AnyOfPredicate<TRecord> Of(IReadOnlyList<IRecordPredicate<TRecord>>? members)
    {
        if (members is null)
        {
            throw new XftyConfigurationException("A predicate list is required.");
        }
        return new AnyOfPredicate<TRecord>(members);
    }

    public bool IsSatisfiedBy(TRecord? record) =>
        this.members.Any(member => member.IsSatisfiedBy(record));
}
