using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="AllOfPredicate{TRecord}"/> - IsSatisfiedBy is true only
/// when every member predicate is (an empty member list is vacuously true),
/// and Of rejects a null list.
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
        Action act = () => AllOfPredicate<Account>.Of(null);

        // Assert
        act.Should().Throw<XftyConfigurationException>().WithMessage("*predicate list is required*");
    }

    private static List<IRecordPredicate<Account>> BigTechPredicates() =>
    [
        FieldPredicateFactory.GreaterThan((Account a) => a.NumberOfEmployees, 100),
        FieldPredicateFactory.EqualTo((Account a) => a.Industry, "Technology")
    ];

    private static void AssertIsSatisfiedBy(
        List<IRecordPredicate<Account>> members,
        Account? record,
        bool expectedResult)
    {
        // Arrange
        IRecordPredicate<Account> predicate = AllOfPredicate<Account>.Of(members);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        actualResult.Should().Be(expectedResult);
    }
}
