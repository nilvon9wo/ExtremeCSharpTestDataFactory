using System.Reflection;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Test.Predicates;

/// <summary>
/// Proves the <see cref="FieldPredicateFactory"/> facade wires each factory
/// method to the right single-field predicate. The predicates' own edge cases
/// are proved in their dedicated test classes; here one representative
/// outcome per factory is enough. NotEqualTo/IsNotNull are a negated EqualTo,
/// so both directions are checked.
/// </summary>
public class FieldPredicateFactoryTest
{
    private static readonly PropertyInfo Industry = Field.Of<Account>(nameof(Account.Industry));
    private static readonly PropertyInfo NumberOfEmployees = Field.Of<Account>(nameof(Account.NumberOfEmployees));

    [Fact]
    public void EqualTo_WhenTheFieldMatches_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.EqualTo(Industry, "Technology"),
            new Account { Industry = "Technology" }, true);

    [Fact]
    public void NotEqualTo_WhenTheFieldDiffers_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.NotEqualTo(Industry, "Retail"),
            new Account { Industry = "Technology" }, true);

    [Fact]
    public void NotEqualTo_WhenTheFieldMatches_ReturnsFalse() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.NotEqualTo(Industry, "Retail"),
            new Account { Industry = "Retail" }, false);

    [Fact]
    public void GreaterThan_WhenTheFieldExceedsTheValue_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.GreaterThan(NumberOfEmployees, 100),
            new Account { NumberOfEmployees = 900 }, true);

    [Fact]
    public void LessThan_WhenTheFieldIsBelowTheValue_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.LessThan(NumberOfEmployees, 100),
            new Account { NumberOfEmployees = 5 }, true);

    [Fact]
    public void IsNull_WhenTheFieldIsBlank_ReturnsTrue() =>
        AssertIsSatisfiedBy(FieldPredicateFactory.IsNull(Industry), new Account(), true);

    [Fact]
    public void IsNotNull_WhenTheFieldIsSet_ReturnsTrue() =>
        AssertIsSatisfiedBy(FieldPredicateFactory.IsNotNull(Industry), new Account { Industry = "Technology" }, true);

    [Fact]
    public void IsNotNull_WhenTheFieldIsBlank_ReturnsFalse() =>
        AssertIsSatisfiedBy(FieldPredicateFactory.IsNotNull(Industry), new Account(), false);

    [Fact]
    public void InSet_WhenTheFieldIsAMember_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            FieldPredicateFactory.InSet(Industry, new object?[] { "Technology" }),
            new Account { Industry = "Technology" }, true);

    private static void AssertIsSatisfiedBy(IRecordPredicate predicate, Account? record, bool expectedResult)
    {
        // Arrange - the caller supplies the facade-built predicate and the record

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
