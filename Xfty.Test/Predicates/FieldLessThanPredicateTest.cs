using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Test.Predicates;

/// <summary>
/// Proves <see cref="FieldLessThanPredicate"/> - IsSatisfiedBy is true
/// exactly when the field orders strictly before the configured value, and
/// false whenever the two cannot be compared. The ordering itself is proved
/// in ValueComparisonTest.
/// </summary>
public class FieldLessThanPredicateTest
{
    [Fact]
    public void IsSatisfiedBy_WhenFieldIsBelowTheValue_ReturnsTrue() =>
        AssertIsSatisfiedBy(100, new Account { NumberOfEmployees = 5 }, true);

    [Fact]
    public void IsSatisfiedBy_WhenFieldEqualsTheValue_ReturnsFalse() =>
        AssertIsSatisfiedBy(100, new Account { NumberOfEmployees = 100 }, false);

    [Fact]
    public void IsSatisfiedBy_WhenFieldExceedsTheValue_ReturnsFalse() =>
        AssertIsSatisfiedBy(100, new Account { NumberOfEmployees = 900 }, false);

    [Fact]
    public void IsSatisfiedBy_WhenFieldValueIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy(1, new Account(), false);

    [Fact]
    public void IsSatisfiedBy_WhenRecordIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy(1, null, false);

    private static void AssertIsSatisfiedBy(object? threshold, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate predicate =
            FieldLessThanPredicate.Of(Field.Of<Account>(nameof(Account.NumberOfEmployees)), threshold);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
