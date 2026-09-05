using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="AnyOfPredicate"/> - IsSatisfiedBy is true when at least
/// one member predicate is (an empty member list is never satisfied), and Of
/// rejects a null list.
/// </summary>
public class AnyOfPredicateTest
{
    [Fact]
    public void IsSatisfiedBy_WhenOneMemberIsSatisfied_ReturnsTrue() =>
        AssertIsSatisfiedBy(BigOrTechPredicates(), new Account { NumberOfEmployees = 10, Industry = "Technology" }, true);

    [Fact]
    public void IsSatisfiedBy_WhenNoMemberIsSatisfied_ReturnsFalse() =>
        AssertIsSatisfiedBy(BigOrTechPredicates(), new Account { NumberOfEmployees = 10, Industry = "Retail" }, false);

    [Fact]
    public void IsSatisfiedBy_WhenTheMemberListIsEmpty_ReturnsFalse() =>
        AssertIsSatisfiedBy([], new Account(), false);

    [Fact]
    public void Of_WhenTheMemberListIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => AnyOfPredicate.Of(null));

        // Assert
        Assert.Contains("predicate list is required", thrown.Message);
    }

    private static List<IRecordPredicate> BigOrTechPredicates() =>
    [
        FieldPredicateFactory.GreaterThan(Field.Of<Account>(nameof(Account.NumberOfEmployees)), 5000),
        FieldPredicateFactory.EqualTo(Field.Of<Account>(nameof(Account.Industry)), "Technology")
    ];

    private static void AssertIsSatisfiedBy(List<IRecordPredicate> members, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate predicate = AnyOfPredicate.Of(members);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
