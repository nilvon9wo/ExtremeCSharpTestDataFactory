namespace Net.NowhereAtAll.Xfty.Predicates;

/// <summary>
/// Discoverable factory for the boolean combinators over
/// <see cref="IRecordMatching"/>. Each result is itself an
/// <see cref="IRecordMatching"/>, so they nest:
///
/// <code>
/// PredicateFactory.AnyOf(new IRecordMatching[] {
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
    public static IRecordMatching AllOf(IReadOnlyList<IRecordMatching> predicates) =>
        AllOfPredicate.Of(predicates);

    /// <summary>Satisfied when at least one member predicate is. An empty list is never satisfied.</summary>
    public static IRecordMatching AnyOf(IReadOnlyList<IRecordMatching> predicates) =>
        AnyOfPredicate.Of(predicates);

    /// <summary>Satisfied exactly when <paramref name="predicate"/> is not.</summary>
    public static IRecordMatching Negate(IRecordMatching predicate) =>
        NegationPredicate.Of(predicate);
}