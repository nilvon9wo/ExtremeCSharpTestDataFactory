using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="FieldInSetPredicate{TRecord,TValue}"/> - IsSatisfiedBy is
/// true exactly when the record's field is one of the configured set; a null
/// set accepts nothing.
/// </summary>
public class FieldInSetPredicateTest
{
    private static readonly string?[] FinanceOrTech = ["Finance", "Technology"];

    [Fact]
    public void IsSatisfiedBy_WhenFieldIsAMemberOfTheSet_ReturnsTrue() =>
        AssertIsSatisfiedBy(FinanceOrTech, new Account { Industry = "Technology" }, true);

    [Fact]
    public void IsSatisfiedBy_WhenFieldIsNotAMemberOfTheSet_ReturnsFalse() =>
        AssertIsSatisfiedBy(FinanceOrTech, new Account { Industry = "Retail" }, false);

    [Fact]
    public void IsSatisfiedBy_WhenTheSetIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy(null, new Account { Industry = "Technology" }, false);

    [Fact]
    public void IsSatisfiedBy_WhenTheRecordIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy(FinanceOrTech, null, false);

    private static void AssertIsSatisfiedBy(string?[]? acceptedValues, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate<Account> predicate = FieldInSetPredicate<Account, string?>.Of(a => a.Industry, acceptedValues);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        actualResult.Should().Be(expectedResult);
    }
}
