using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="NegationPredicate"/> - IsSatisfiedBy returns the
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
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => NegationPredicate.Of(null));

        // Assert
        Assert.Contains("predicate to negate is required", thrown.Message);
    }

    private static void AssertIsSatisfiedBy(Account? record, bool expectedResult)
    {
        // Arrange - negate "Type is Prospect"
        IRecordPredicate predicate =
            NegationPredicate.Of(FieldPredicateFactory.EqualTo(Field.Of<Account>(nameof(Account.Type)), "Prospect"));

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
