namespace Net.Nowhereatall.Xfty.Predicates;

/// <summary>
/// An arbitrary condition on a record. Conditions need not be equality -
/// "annual revenue over 1M", "industry in a set", "nickname is null" are all
/// fine.
///
/// Implement this yourself for anything <see cref="FieldPredicateFactory"/>
/// does not express - a one-method interface, no base class, no registration:
///
/// <code>
/// public sealed class WasCreatedOnAWeekday : IRecordPredicate
/// {
///     public bool IsSatisfiedBy(object? record) =>
///         Field.Of&lt;Account&gt;(nameof(Account.CreatedDate)).GetValue(record)
///             is DateTime { DayOfWeek: not DayOfWeek.Saturday and not DayOfWeek.Sunday };
/// }
/// </code>
///
/// See <see cref="FieldPredicateFactory"/> for ready-made single-field
/// conditions and <see cref="PredicateFactory"/> for AND / OR / NOT
/// combinators.
/// </summary>
public interface IRecordPredicate
{
    bool IsSatisfiedBy(object? record);
}
