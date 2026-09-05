using Net.Nowhereatall.Xfty.Core;
namespace Net.Nowhereatall.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied when at least one member
/// predicate is (logical OR). An empty member list is never satisfied.
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class AnyOfPredicate : IRecordPredicate
{
    private readonly IReadOnlyList<IRecordPredicate> members;

    private AnyOfPredicate(IReadOnlyList<IRecordPredicate> members) => this.members = members;

    public static AnyOfPredicate Of(IReadOnlyList<IRecordPredicate>? members) =>
        members is null
            ? throw new XftyConfigurationException("A predicate list is required.")
            : new AnyOfPredicate(members);

    public bool IsSatisfiedBy(object? record) =>
        this.members.Any(member => member.IsSatisfiedBy(record));
}
