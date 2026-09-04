using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="FieldLessThanPredicate{TRecord,TValue}"/> - IsSatisfiedBy
/// is true exactly when the field orders strictly before the configured
/// value, and false whenever the two cannot be compared.
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

    private static void AssertIsSatisfiedBy(int? threshold, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate<Account> predicate =
            FieldLessThanPredicate<Account, int?>.Of(a => a.NumberOfEmployees, threshold);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        actualResult.Should().Be(expectedResult);
    }
}
