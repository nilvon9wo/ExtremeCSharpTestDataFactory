using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="FieldGreaterThanPredicate{TRecord,TValue}"/> - IsSatisfiedBy
/// is true exactly when the field orders strictly after the configured value,
/// and false whenever the two cannot be compared.
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

    private static void AssertIsSatisfiedBy(int? threshold, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate<Account> predicate =
            FieldGreaterThanPredicate<Account, int?>.Of(a => a.NumberOfEmployees, threshold);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        actualResult.Should().Be(expectedResult);
    }
}
