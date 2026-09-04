namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// Discoverable factory for the ready-made single-field
/// <see cref="IRecordPredicate{TRecord}"/> conditions. Each call returns a
/// plain predicate you can combine with <see cref="PredicateFactory"/> or
/// evaluate directly:
///
/// <code>
/// IRecordPredicate&lt;Account&gt; isBigTech =
///     PredicateFactory.AllOf(new[] {
///         FieldPredicateFactory.GreaterThan((Account a) => a.AnnualRevenue, 1_000_000m),
///         FieldPredicateFactory.EqualTo((Account a) => a.Industry, "Technology")
///     });
/// </code>
///
/// These cover the common cases only. Implement
/// <see cref="IRecordPredicate{TRecord}"/> yourself for anything these do not
/// express. Each factory just wires up a purpose-built class - <see cref="EqualTo"/>
/// to <see cref="FieldEqualToPredicate{TRecord,TValue}"/>, and so on - and
/// <see cref="NotEqualTo"/>/<see cref="IsNotNull"/> are just a negated
/// <see cref="EqualTo"/>. Use those classes directly if you prefer; this
/// facade only saves an import.
/// </summary>
public static class FieldPredicateFactory
{
    public static IRecordPredicate<TRecord> EqualTo<TRecord, TValue>(
        Func<TRecord, TValue> field,
        TValue? comparisonValue)
        where TRecord : class =>
        FieldEqualToPredicate<TRecord, TValue>.Of(field, comparisonValue);

    public static IRecordPredicate<TRecord> NotEqualTo<TRecord, TValue>(
        Func<TRecord, TValue> field,
        TValue? comparisonValue)
        where TRecord : class =>
        NegationPredicate<TRecord>.Of(EqualTo(field, comparisonValue));

    public static IRecordPredicate<TRecord> GreaterThan<TRecord, TValue>(
        Func<TRecord, TValue> field,
        TValue? comparisonValue)
        where TRecord : class =>
        FieldGreaterThanPredicate<TRecord, TValue>.Of(field, comparisonValue);

    public static IRecordPredicate<TRecord> LessThan<TRecord, TValue>(
        Func<TRecord, TValue> field,
        TValue? comparisonValue)
        where TRecord : class =>
        FieldLessThanPredicate<TRecord, TValue>.Of(field, comparisonValue);

    public static IRecordPredicate<TRecord> IsNull<TRecord, TValue>(Func<TRecord, TValue> field)
        where TRecord : class =>
        EqualTo(field, default);

    public static IRecordPredicate<TRecord> IsNotNull<TRecord, TValue>(Func<TRecord, TValue> field)
        where TRecord : class =>
        NegationPredicate<TRecord>.Of(IsNull(field));

    public static IRecordPredicate<TRecord> InSet<TRecord, TValue>(
        Func<TRecord, TValue> field,
        IEnumerable<TValue?>? acceptedValues)
        where TRecord : class =>
        FieldInSetPredicate<TRecord, TValue>.Of(field, acceptedValues);
}
