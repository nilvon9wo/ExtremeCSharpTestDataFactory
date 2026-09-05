namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied only when every member
/// predicate is (logical AND). An empty member list is vacuously satisfied.
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class AllOfPredicate : IRecordPredicate
{
    private readonly IReadOnlyList<IRecordPredicate> members;

    private AllOfPredicate(IReadOnlyList<IRecordPredicate> members)
    {
        this.members = members;
    }

    public static AllOfPredicate Of(IReadOnlyList<IRecordPredicate>? members)
    {
        if (members is null)
        {
            throw new XftyConfigurationException("A predicate list is required.");
        }
        return new AllOfPredicate(members);
    }

    public bool IsSatisfiedBy(object? record) =>
        this.members.All(member => member.IsSatisfiedBy(record));
}
