using System.Linq.Expressions;
using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Predicates;

/// <summary>
/// Discoverable factory for the ready-made single-field
/// <see cref="IRecordMatching"/> conditions. Each call returns a plain
/// predicate you can combine with <see cref="PredicateFactory"/> or evaluate
/// directly:
///
/// <code>
/// IRecordMatching isBigTech = PredicateFactory.AllOf(new[] {
///     FieldPredicateFactory.GreaterThan(Field.Of&lt;Account&gt;(nameof(Account.AnnualRevenue)), 1_000_000m),
///     FieldPredicateFactory.EqualTo(Field.Of&lt;Account&gt;(nameof(Account.Industry)), "Technology")
/// });
/// </code>
///
/// These cover the common cases only. Implement <see cref="IRecordMatching"/>
/// yourself for anything these do not express. Each factory just wires up a
/// purpose-built class - <see cref="EqualTo"/> to <see cref="FieldEqualToPredicate"/>,
/// and so on - and <see cref="NotEqualTo"/>/<see cref="IsNotNull"/> are just a
/// negated <see cref="EqualTo"/>. Use those classes directly if you prefer;
/// this facade only saves an import.
/// </summary>
public static class FieldPredicateFactory
{
    public static IRecordMatching EqualTo(PropertyInfo field, object? comparisonValue) =>
        FieldEqualToPredicate.Of(field, comparisonValue);

    public static IRecordMatching NotEqualTo(PropertyInfo field, object? comparisonValue) =>
        NegationPredicate.Of(EqualTo(field, comparisonValue));

    public static IRecordMatching GreaterThan(PropertyInfo field, object? comparisonValue) =>
        FieldGreaterThanPredicate.Of(field, comparisonValue);

    public static IRecordMatching LessThan(PropertyInfo field, object? comparisonValue) =>
        FieldLessThanPredicate.Of(field, comparisonValue);

    public static IRecordMatching IsNull(PropertyInfo field) =>
        EqualTo(field, null);

    public static IRecordMatching IsNotNull(PropertyInfo field) =>
        NegationPredicate.Of(IsNull(field));

    public static IRecordMatching InSet(PropertyInfo field, IEnumerable<object?>? acceptedValues) =>
        FieldInSetPredicate.Of(field, acceptedValues);

    // Lambda overloads - naming field by lambda instead of Field.Of<TRecord>(...) --------

    public static IRecordMatching EqualTo<TRecord>(
        Expression<Func<TRecord, object?>> field,
        object? comparisonValue
    ) =>
        EqualTo(Field.Of(field), comparisonValue);

    public static IRecordMatching NotEqualTo<TRecord>(
        Expression<Func<TRecord, object?>> field,
        object? comparisonValue
    ) =>
        NotEqualTo(Field.Of(field), comparisonValue);

    public static IRecordMatching GreaterThan<TRecord>(
        Expression<Func<TRecord, object?>> field,
        object? comparisonValue
    ) =>
        GreaterThan(Field.Of(field), comparisonValue);

    public static IRecordMatching LessThan<TRecord>(
        Expression<Func<TRecord, object?>> field,
        object? comparisonValue
    ) =>
        LessThan(Field.Of(field), comparisonValue);

    public static IRecordMatching IsNull<TRecord>(Expression<Func<TRecord, object?>> field) =>
        IsNull(Field.Of(field));

    public static IRecordMatching IsNotNull<TRecord>(Expression<Func<TRecord, object?>> field) =>
        IsNotNull(Field.Of(field));

    public static IRecordMatching InSet<TRecord>(
        Expression<Func<TRecord, object?>> field,
        IEnumerable<object?>? acceptedValues
    ) =>
        InSet(Field.Of(field), acceptedValues);
}