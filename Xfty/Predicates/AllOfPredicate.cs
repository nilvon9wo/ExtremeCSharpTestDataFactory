using Net.NowhereAtAll.Xfty.Core;
namespace Net.NowhereAtAll.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordMatching"/> satisfied only when every member
/// predicate is (logical AND). An empty member list is vacuously satisfied.
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class AllOfPredicate : IRecordMatching
{
    private readonly IReadOnlyList<IRecordMatching> _members;

    private AllOfPredicate(IReadOnlyList<IRecordMatching> members) => this._members = members;

    public static AllOfPredicate Of(IReadOnlyList<IRecordMatching>? members) =>
        members is null
            ? throw new XftyConfigurationException("A predicate list is required.")
            : new AllOfPredicate(members);

    public bool IsSatisfiedBy(object? record) =>
        this._members.All(member => member.IsSatisfiedBy(record));
}