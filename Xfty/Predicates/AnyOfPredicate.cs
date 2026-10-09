using Net.NowhereAtAll.Xfty.Core;
namespace Net.NowhereAtAll.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordMatching"/> satisfied when at least one member
/// predicate is (logical OR). An empty member list is never satisfied.
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class AnyOfPredicate : IRecordMatching
{
    private readonly IReadOnlyList<IRecordMatching> _members;

    private AnyOfPredicate(IReadOnlyList<IRecordMatching> members) => this._members = members;

    public static AnyOfPredicate Of(IReadOnlyList<IRecordMatching>? members) =>
        members is null
            ? throw new XftyConfigurationException("A predicate list is required.")
            : new AnyOfPredicate(members);

    public bool IsSatisfiedBy(object? record) =>
        this._members.Any(member => member.IsSatisfiedBy(record));
}