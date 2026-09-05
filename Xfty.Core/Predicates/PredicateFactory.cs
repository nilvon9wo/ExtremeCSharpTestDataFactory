namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// Discoverable factory for the boolean combinators over
/// <see cref="IRecordPredicate"/>. Each result is itself an
/// <see cref="IRecordPredicate"/>, so they nest:
///
/// <code>
/// PredicateFactory.AnyOf(new IRecordPredicate[] {
///     FieldPredicateFactory.GreaterThan(Field.Of&lt;Account&gt;(nameof(Account.AnnualRevenue)), 1_000_000m),
///     FieldPredicateFactory.GreaterThan(Field.Of&lt;Account&gt;(nameof(Account.NumberOfEmployees)), 5000)
/// });
/// </code>
///
/// The implementations are <see cref="AllOfPredicate"/>,
/// <see cref="AnyOfPredicate"/> and <see cref="NegationPredicate"/> - use
/// those directly if you prefer; this facade only saves an import.
/// </summary>
public static class PredicateFactory
{
    /// <summary>Satisfied only when every member predicate is. An empty list is vacuously satisfied.</summary>
    public static IRecordPredicate AllOf(IReadOnlyList<IRecordPredicate> predicates) =>
        AllOfPredicate.Of(predicates);

    /// <summary>Satisfied when at least one member predicate is. An empty list is never satisfied.</summary>
    public static IRecordPredicate AnyOf(IReadOnlyList<IRecordPredicate> predicates) =>
        AnyOfPredicate.Of(predicates);

    /// <summary>Satisfied exactly when <paramref name="predicate"/> is not.</summary>
    public static IRecordPredicate Negate(IRecordPredicate predicate) =>
        NegationPredicate.Of(predicate);
}
