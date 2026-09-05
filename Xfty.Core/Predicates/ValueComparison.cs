using System.Globalization;
using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// Orders two values the way the ordering field predicates
/// (<see cref="FieldGreaterThanPredicate"/>, <see cref="FieldLessThanPredicate"/>)
/// need: numbers numerically, dates/times chronologically, everything else
/// lexicographically.
/// </summary>
public static class ValueComparison
{
    /// <summary>
    /// -1 / 0 / 1 comparing <paramref name="record"/>'s <paramref name="field"/>
    /// against <paramref name="value"/>, or null when either side is absent
    /// (so the caller can decide what an incomparable pair means).
    /// </summary>
    public static int? FieldToValue(object? record, PropertyInfo field, object? value)
    {
        object? actual = record is null
            ? null
            : field.GetValue(record);
        return actual is null || value is null
            ? null
            : Compare(actual, value);
    }

    /// <summary>-1 / 0 / 1. Both arguments must be non-null and of comparable kinds.</summary>
    public static int Compare(object left, object right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            return Math.Sign(ToDecimal(left).CompareTo(ToDecimal(right)));
        }
        if (left is DateTime leftMoment && right is DateTime rightMoment)
        {
            return Math.Sign(leftMoment.CompareTo(rightMoment));
        }
        string leftText = Convert.ToString(left, CultureInfo.InvariantCulture) ?? string.Empty;
        string rightText = Convert.ToString(right, CultureInfo.InvariantCulture) ?? string.Empty;
        return Math.Sign(string.CompareOrdinal(leftText, rightText));
    }

    private static bool IsNumeric(object value) =>
        value is decimal or int or long or double or float;

    private static decimal ToDecimal(object value) =>
        Convert.ToDecimal(value, CultureInfo.InvariantCulture);
}
