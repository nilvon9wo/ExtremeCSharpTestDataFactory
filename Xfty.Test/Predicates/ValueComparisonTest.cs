using System.Reflection;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Test.Predicates;

/// <summary>
/// Proves <see cref="ValueComparison"/>: Compare returns the sign of the
/// natural ordering (numbers numerically, dates/times chronologically,
/// otherwise lexicographically), and FieldToValue returns null for any
/// pairing that cannot be ordered.
/// </summary>
public class ValueComparisonTest
{
    private static readonly DateTime Noon = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly PropertyInfo NumberOfEmployees = Field.Of<Account>(nameof(Account.NumberOfEmployees));

    // Compare(left, right) -------------------------------------------

    [Fact]
    public void Compare_WhenTheLeftNumberIsGreater_ReturnsPositive() =>
        AssertCompare(900, 100, 1);

    [Fact]
    public void Compare_WhenTheNumbersAreEqual_ReturnsZero() =>
        AssertCompare(100, 100, 0);

    [Fact]
    public void Compare_WhenTheLeftNumberIsSmaller_ReturnsNegative() =>
        AssertCompare(5, 100, -1);

    [Fact]
    public void Compare_WhenGivenDecimals_OrdersThemNumerically() =>
        AssertCompare(1000000.50m, 1000000.25m, 1);

    [Fact]
    public void Compare_WhenTheLeftMomentIsLater_ReturnsPositive() =>
        AssertCompare(Noon.AddHours(1), Noon, 1);

    [Fact]
    public void Compare_WhenTheLeftMomentIsEarlier_ReturnsNegative() =>
        AssertCompare(Noon.AddHours(-1), Noon, -1);

    [Fact]
    public void Compare_WhenGivenStrings_OrdersThemLexicographically() =>
        AssertCompare("Acme", "Aardvark", 1);

    // FieldToValue(record, field, value) ------------------------------

    [Fact]
    public void FieldToValue_WhenTheFieldIsAbsent_ReturnsNull() =>
        AssertFieldToValue(new Account(), 100, null);

    [Fact]
    public void FieldToValue_WhenTheComparisonValueIsNull_ReturnsNull() =>
        AssertFieldToValue(new Account { NumberOfEmployees = 5 }, null, null);

    [Fact]
    public void FieldToValue_WhenTheRecordIsNull_ReturnsNull() =>
        AssertFieldToValue(null, 100, null);

    [Fact]
    public void FieldToValue_WhenBothSidesArePresent_ReturnsTheirOrdering() =>
        AssertFieldToValue(new Account { NumberOfEmployees = 900 }, 100, 1);

    // Helpers ----------------------------------------------------------

    private static void AssertCompare(object left, object right, int expectedSign)
    {
        // Arrange - the caller supplies the pair to order

        // Act
        int actualSign = ValueComparison.Compare(left, right);

        // Assert
        Assert.Equal(expectedSign, actualSign);
    }

    private static void AssertFieldToValue(Account? record, object? comparisonValue, int? expectedResult)
    {
        // Arrange - the caller supplies the record and comparison value

        // Act
        int? actualResult = ValueComparison.FieldToValue(record, NumberOfEmployees, comparisonValue);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
