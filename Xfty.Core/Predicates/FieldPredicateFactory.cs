using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// Discoverable factory for the ready-made single-field
/// <see cref="IRecordPredicate"/> conditions. Each call returns a plain
/// predicate you can combine with <see cref="PredicateFactory"/> or evaluate
/// directly:
///
/// <code>
/// IRecordPredicate isBigTech = PredicateFactory.AllOf(new[] {
///     FieldPredicateFactory.GreaterThan(Field.Of&lt;Account&gt;(nameof(Account.AnnualRevenue)), 1_000_000m),
///     FieldPredicateFactory.EqualTo(Field.Of&lt;Account&gt;(nameof(Account.Industry)), "Technology")
/// });
/// </code>
///
/// These cover the common cases only. Implement <see cref="IRecordPredicate"/>
/// yourself for anything these do not express. Each factory just wires up a
/// purpose-built class - <see cref="EqualTo"/> to <see cref="FieldEqualToPredicate"/>,
/// and so on - and <see cref="NotEqualTo"/>/<see cref="IsNotNull"/> are just a
/// negated <see cref="EqualTo"/>. Use those classes directly if you prefer;
/// this facade only saves an import.
/// </summary>
public static class FieldPredicateFactory
{
    public static IRecordPredicate EqualTo(PropertyInfo field, object? comparisonValue) =>
        FieldEqualToPredicate.Of(field, comparisonValue);

    public static IRecordPredicate NotEqualTo(PropertyInfo field, object? comparisonValue) =>
        NegationPredicate.Of(EqualTo(field, comparisonValue));

    public static IRecordPredicate GreaterThan(PropertyInfo field, object? comparisonValue) =>
        FieldGreaterThanPredicate.Of(field, comparisonValue);

    public static IRecordPredicate LessThan(PropertyInfo field, object? comparisonValue) =>
        FieldLessThanPredicate.Of(field, comparisonValue);

    public static IRecordPredicate IsNull(PropertyInfo field) =>
        EqualTo(field, null);

    public static IRecordPredicate IsNotNull(PropertyInfo field) =>
        NegationPredicate.Of(IsNull(field));

    public static IRecordPredicate InSet(PropertyInfo field, IEnumerable<object?>? acceptedValues) =>
        FieldInSetPredicate.Of(field, acceptedValues);
}
