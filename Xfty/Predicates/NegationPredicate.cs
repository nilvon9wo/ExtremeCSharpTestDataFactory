using Net.NowhereAtAll.Xfty.Core;
namespace Net.NowhereAtAll.Xfty.Predicates;

/// <summary>
/// An <see cref="IRecordMatching"/> satisfied exactly when the predicate it
/// wraps is not (logical NOT).
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class NegationPredicate : IRecordMatching
{
    private readonly IRecordMatching _negated;

    private NegationPredicate(IRecordMatching negated) => this._negated = negated;

    public static NegationPredicate Of(IRecordMatching? predicate) =>
        predicate is null
            ? throw new XftyConfigurationException("A predicate to negate is required.")
            : new NegationPredicate(predicate);

    public bool IsSatisfiedBy(object? record) =>
        !this._negated.IsSatisfiedBy(record);
}