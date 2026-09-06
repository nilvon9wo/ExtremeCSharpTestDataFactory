using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Predicates;

namespace Net.NowhereAtAll.Xfty.Test.Predicates;

/// <summary>
/// Proves <see cref="FieldInSetPredicate"/> - IsSatisfiedBy is true exactly
/// when the record's field is one of the configured set; a null set accepts
/// nothing.
/// </summary>
public class FieldInSetPredicateTest
{
    private static readonly object?[] FinanceOrTech = ["Finance", "Technology"];

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

    private static void AssertIsSatisfiedBy(object?[]? acceptedValues, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate predicate = FieldInSetPredicate.Of(Field.Of<Account>(x => x.Industry), acceptedValues);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
