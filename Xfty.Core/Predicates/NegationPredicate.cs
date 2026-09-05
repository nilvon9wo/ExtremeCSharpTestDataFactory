using Net.Nowhereatall.Xfty.Core.Core;
namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// An <see cref="IRecordPredicate"/> satisfied exactly when the predicate it
/// wraps is not (logical NOT).
///
/// Obtain one through <see cref="Of"/> or the <see cref="PredicateFactory"/>
/// facade.
/// </summary>
public sealed class NegationPredicate : IRecordPredicate
{
    private readonly IRecordPredicate negated;

    private NegationPredicate(IRecordPredicate negated) => this.negated = negated;

    public static NegationPredicate Of(IRecordPredicate? predicate) =>
        predicate is null
            ? throw new XftyConfigurationException("A predicate to negate is required.")
            : new NegationPredicate(predicate);

    public bool IsSatisfiedBy(object? record) =>
        !this.negated.IsSatisfiedBy(record);
}
