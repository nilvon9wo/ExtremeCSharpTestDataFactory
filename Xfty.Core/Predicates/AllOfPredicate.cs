namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate{TRecord}"/> satisfied only when every member
/// predicate is (logical AND). An empty member list is vacuously satisfied.
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class AllOfPredicate<TRecord> : IRecordPredicate<TRecord>
{
    private readonly IReadOnlyList<IRecordPredicate<TRecord>> members;

    private AllOfPredicate(IReadOnlyList<IRecordPredicate<TRecord>> members)
    {
        this.members = members;
    }

    public static AllOfPredicate<TRecord> Of(IReadOnlyList<IRecordPredicate<TRecord>>? members)
    {
        if (members is null)
        {
            throw new XftyConfigurationException("A predicate list is required.");
        }
        return new AllOfPredicate<TRecord>(members);
    }

    public bool IsSatisfiedBy(TRecord? record) =>
        this.members.All(member => member.IsSatisfiedBy(record));
}
