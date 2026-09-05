using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="FieldEqualToPredicate"/> - IsSatisfiedBy is true exactly
/// when the record's field equals the configured value, null included.
/// </summary>
public class FieldEqualToPredicateTest
{
    [Fact]
    public void IsSatisfiedBy_WhenFieldEqualsValue_ReturnsTrue() =>
        AssertIsSatisfiedBy("Technology", new Account { Industry = "Technology" }, true);

    [Fact]
    public void IsSatisfiedBy_WhenFieldDiffersFromValue_ReturnsFalse() =>
        AssertIsSatisfiedBy("Technology", new Account { Industry = "Retail" }, false);

    [Fact]
    public void IsSatisfiedBy_WhenConfiguredWithNullAndFieldIsBlank_ReturnsTrue() =>
        AssertIsSatisfiedBy(null, new Account(), true);

    [Fact]
    public void IsSatisfiedBy_WhenConfiguredWithNullAndFieldIsSet_ReturnsFalse() =>
        AssertIsSatisfiedBy(null, new Account { Industry = "Technology" }, false);

    [Fact]
    public void IsSatisfiedBy_WhenRecordIsNull_ReturnsFalse() =>
        AssertIsSatisfiedBy("Technology", null, false);

    private static void AssertIsSatisfiedBy(object? configuredValue, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate predicate = FieldEqualToPredicate.Of(Field.Of<Account>(nameof(Account.Industry)), configuredValue);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
