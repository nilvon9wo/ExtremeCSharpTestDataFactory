using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Test.Predicates;

/// <summary>
/// Proves <see cref="AllOfPredicate"/> - IsSatisfiedBy is true only when every
/// member predicate is (an empty member list is vacuously true), and Of
/// rejects a null list.
/// </summary>
public class AllOfPredicateTest
{
    [Fact]
    public void IsSatisfiedBy_WhenEveryMemberIsSatisfied_ReturnsTrue() =>
        AssertIsSatisfiedBy(BigTechPredicates(), new Account { NumberOfEmployees = 900, Industry = "Technology" }, true);

    [Fact]
    public void IsSatisfiedBy_WhenOneMemberIsNotSatisfied_ReturnsFalse() =>
        AssertIsSatisfiedBy(BigTechPredicates(), new Account { NumberOfEmployees = 900, Industry = "Retail" }, false);

    [Fact]
    public void IsSatisfiedBy_WhenTheMemberListIsEmpty_ReturnsTrue() =>
        AssertIsSatisfiedBy([], new Account(), true);

    [Fact]
    public void Of_WhenTheMemberListIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => AllOfPredicate.Of(null));

        // Assert
        Assert.Contains("predicate list is required", thrown.Message);
    }

    private static List<IRecordPredicate> BigTechPredicates() =>
    [
        FieldPredicateFactory.GreaterThan<Account>(x => x.NumberOfEmployees, 100),
        FieldPredicateFactory.EqualTo<Account>(x => x.Industry, "Technology")
    ];

    private static void AssertIsSatisfiedBy(List<IRecordPredicate> members, Account? record, bool expectedResult)
    {
        // Arrange
        IRecordPredicate predicate = AllOfPredicate.Of(members);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
