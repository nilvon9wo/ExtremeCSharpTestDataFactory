using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Predicates;

namespace Net.NowhereAtAll.Xfty.Test.Predicates;

/// <summary>
/// Proves the <see cref="PredicateFactory"/> combinator facade wires
/// AllOf/AnyOf/Negate to the right implementation.
/// </summary>
public class PredicateFactoryTest
{
    [Fact]
    public void AllOf_WhenAMemberIsNotSatisfied_ReturnsFalse() =>
        AssertIsSatisfiedBy(
            PredicateFactory.AllOf([FieldPredicateFactory.EqualTo<Account>(x => x.Industry, "Technology")]),
            new Account { Industry = "Retail" }, false);

    [Fact]
    public void AnyOf_WhenAMemberIsSatisfied_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            PredicateFactory.AnyOf([FieldPredicateFactory.EqualTo<Account>(x => x.Industry, "Technology")]),
            new Account { Industry = "Technology" }, true);

    [Fact]
    public void Negate_WhenTheInnerPredicateIsNotSatisfied_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            PredicateFactory.Negate(FieldPredicateFactory.EqualTo<Account>(x => x.Type, "Prospect")),
            new Account { Type = "Customer" }, true);

    private static void AssertIsSatisfiedBy(IRecordPredicate predicate, Account? record, bool expectedResult)
    {
        // Arrange - the caller supplies the facade-built predicate and the record

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
