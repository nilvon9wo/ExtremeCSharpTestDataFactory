using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="NegationPredicate{TRecord}"/> - IsSatisfiedBy returns the
/// opposite of the wrapped predicate, and Of rejects a null predicate.
/// </summary>
public class NegationPredicateTest
{
    [Fact]
    public void IsSatisfiedBy_WhenTheInnerPredicateIsSatisfied_ReturnsFalse() =>
        AssertIsSatisfiedBy(new Account { Type = "Prospect" }, false);

    [Fact]
    public void IsSatisfiedBy_WhenTheInnerPredicateIsNotSatisfied_ReturnsTrue() =>
        AssertIsSatisfiedBy(new Account { Type = "Customer" }, true);

    [Fact]
    public void Of_WhenThePredicateIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        Action act = () => NegationPredicate<Account>.Of(null);

        // Assert
        act.Should().Throw<XftyConfigurationException>().WithMessage("*predicate to negate is required*");
    }

    private static void AssertIsSatisfiedBy(Account? record, bool expectedResult)
    {
        // Arrange - negate "Type is Prospect"
        IRecordPredicate<Account> predicate =
            NegationPredicate<Account>.Of(FieldPredicateFactory.EqualTo((Account a) => a.Type, "Prospect"));

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        actualResult.Should().Be(expectedResult);
    }
}
