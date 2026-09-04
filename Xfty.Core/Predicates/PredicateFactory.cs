namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// Discoverable factory for the boolean combinators over
/// <see cref="IRecordPredicate{TRecord}"/>. Each result is itself an
/// <see cref="IRecordPredicate{TRecord}"/>, so they nest:
///
/// <code>
/// PredicateFactory.AnyOf(new IRecordPredicate&lt;Account&gt;[] {
///     FieldPredicateFactory.GreaterThan((Account a) => a.AnnualRevenue, 1_000_000m),
///     FieldPredicateFactory.GreaterThan((Account a) => a.NumberOfEmployees, 5000)
/// });
/// </code>
///
/// The implementations are <see cref="AllOfPredicate{TRecord}"/>,
/// <see cref="AnyOfPredicate{TRecord}"/> and <see cref="NegationPredicate{TRecord}"/>
/// - use those directly if you prefer; this facade only saves an import.
/// </summary>
public static class PredicateFactory
{
    /// <summary>Satisfied only when every member predicate is. An empty list is vacuously satisfied.</summary>
    public static IRecordPredicate<TRecord> AllOf<TRecord>(IReadOnlyList<IRecordPredicate<TRecord>> predicates) =>
        AllOfPredicate<TRecord>.Of(predicates);

    /// <summary>Satisfied when at least one member predicate is. An empty list is never satisfied.</summary>
    public static IRecordPredicate<TRecord> AnyOf<TRecord>(IReadOnlyList<IRecordPredicate<TRecord>> predicates) =>
        AnyOfPredicate<TRecord>.Of(predicates);

    /// <summary>Satisfied exactly when <paramref name="predicate"/> is not.</summary>
    public static IRecordPredicate<TRecord> Negate<TRecord>(IRecordPredicate<TRecord> predicate) =>
        NegationPredicate<TRecord>.Of(predicate);
}
