using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves the <see cref="FieldPredicateFactory"/> facade wires each factory
/// method to the right single-field predicate. The predicates' own edge cases
/// are proved in their dedicated test classes; here one representative
/// outcome per factory is enough. NotEqualTo/IsNotNull are a negated
/// EqualTo, so both directions are checked.
/// </summary>
public class FieldPredicateFactoryTest
{
    [Fact]
    public void EqualTo_WhenTheFieldMatches_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.EqualTo((Account a) => a.Industry, "Technology"),
            new Account { Industry = "Technology" }, true);

    [Fact]
    public void NotEqualTo_WhenTheFieldDiffers_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.NotEqualTo((Account a) => a.Industry, "Retail"),
            new Account { Industry = "Technology" }, true);

    [Fact]
    public void NotEqualTo_WhenTheFieldMatches_ReturnsFalse() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.NotEqualTo((Account a) => a.Industry, "Retail"),
            new Account { Industry = "Retail" }, false);

    [Fact]
    public void GreaterThan_WhenTheFieldExceedsTheValue_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.GreaterThan((Account a) => a.NumberOfEmployees, 100),
            new Account { NumberOfEmployees = 900 }, true);

    [Fact]
    public void LessThan_WhenTheFieldIsBelowTheValue_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.LessThan((Account a) => a.NumberOfEmployees, 100),
            new Account { NumberOfEmployees = 5 }, true);

    [Fact]
    public void IsNull_WhenTheFieldIsBlank_ReturnsTrue() =>
        AssertIsSatisfiedBy(FieldPredicateFactory.IsNull((Account a) => a.Industry), new Account(), true);

    [Fact]
    public void IsNotNull_WhenTheFieldIsSet_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.IsNotNull((Account a) => a.Industry),
            new Account { Industry = "Technology" }, true);

    [Fact]
    public void IsNotNull_WhenTheFieldIsBlank_ReturnsFalse() =>
        AssertIsSatisfiedBy(FieldPredicateFactory.IsNotNull((Account a) => a.Industry), new Account(), false);

    [Fact]
    public void InSet_WhenTheFieldIsAMember_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.InSet((Account a) => a.Industry, new string?[] { "Technology" }),
            new Account { Industry = "Technology" }, true);

    private static void AssertIsSatisfiedBy(IRecordPredicate<Account> predicate, Account? record, bool expectedResult)
    {
        // Arrange - the caller supplies the facade-built predicate and the record

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        actualResult.Should().Be(expectedResult);
    }
}
