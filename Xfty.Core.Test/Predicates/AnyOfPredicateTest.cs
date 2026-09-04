using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Predicates;

namespace Net.Nowhereatall.Xfty.Core.Test.Predicates;

/// <summary>
/// Proves <see cref="AnyOfPredicate{TRecord}"/> - IsSatisfiedBy is true when
/// at least one member predicate is (an empty member list is never
/// satisfied), and Of rejects a null list.
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
        Action act = () => AnyOfPredicate<Account>.Of(null);

        // Assert
        act.Should().Throw<XftyConfigurationException>().WithMessage("*predicate list is required*");
    }

    private static List<IRecordPredicate<Account>> BigOrTechPredicates() =>
    [
        FieldPredicateFactory.GreaterThan((Account a) => a.NumberOfEmployees, 5000),
        FieldPredicateFactory.EqualTo((Account a) => a.Industry, "Technology")
    ];

    private static void AssertIsSatisfiedBy(
        List<IRecordPredicate<Account>> members,
        Account? record,
        bool expectedResult)
    {
        // Arrange
        IRecordPredicate<Account> predicate = AnyOfPredicate<Account>.Of(members);

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        actualResult.Should().Be(expectedResult);
    }
}
