using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Predicates;

namespace Net.NowhereAtAll.Xfty.Test.Predicates;

/// <summary>
/// Proves <see cref="FieldGreaterThanPredicate"/> - IsSatisfiedBy is true
/// exactly when the field orders strictly after the configured value, and
/// false whenever the two cannot be compared. The ordering itself is
/// <see cref="ValueComparison"/>'s job and is proved in ValueComparisonTest.
/// </summary>
public class FieldGreaterThanPredicateTest
{
    [Fact]
    public void IsSatisfiedBy_WhenFieldExceedsTheValue_ReturnsTrue() =>
        AssertIsSatisfiedBy(100, new Account { NumberOfEmployees = 900 }, true);

    [Fact]
    public void IsSatisfiedBy_WhenFieldEqualsTheValue_ReturnsFalse() =>
        AssertIsSatisfiedBy(900, new Account { NumberOfEmployees = 900 }, false);

    [Fact]
    public void IsSatisfiedBy_WhenFieldIsBelowTheValue_ReturnsFalse() =>
        AssertIsSatisfiedBy(900, new Account { NumberOfEmployees = 5 }, false);

    [Fact]
    public void IsSatisfiedBy_WhenFieldValueIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy(1, new Account(), false);

    [Fact]
    public void IsSatisfiedBy_WhenComparisonValueIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy(null, new Account { NumberOfEmployees = 5 }, false);

    [Fact]
    public void IsSatisfiedBy_WhenRecordIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy(1, null, false);

    private static void AssertIsSatisfiedBy(object? threshold, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate predicate =
            FieldGreaterThanPredicate.Of(Field.Of<Account>(x => x.NumberOfEmployees), threshold);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
