using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>Proves InverseAlignment - for each parent, the children pointing back at it. Pure in-memory, no database access.</summary>
public class InverseAlignmentTest
{
    [Fact]
    public void ChildrenPerParent_WhenParentsHaveIds_MatchesOnTheForeignKey()
    {
        // Arrange
        string one = IdMocker.GenerateId();
        string two = IdMocker.GenerateId();
        List<object> parents = [new Account { Id = one }, new Account { Id = two }];
        List<object> children = [new Contact { AccountId = two }, new Contact { AccountId = one }, new Contact { AccountId = two }];

        // Act
        List<List<object>> perParent = InverseAlignment.ChildrenPerParent(parents, children, Field.Of<Contact>(x => x.AccountId));

        // Assert
        _ = Assert.Single(perParent[0]); // parent one has one child
        Assert.Equal(2, perParent[1].Count); // parent two has two
    }

    [Fact]
    public void ChildrenPerParent_WhenParentsHaveNoIds_MatchesByPosition()
    {
        // Arrange
        List<object> parents = [new Account(), new Account()];
        List<object> children = [new Contact { LastName = "A" }, new Contact { LastName = "B" }];

        // Act
        List<List<object>> perParent = InverseAlignment.ChildrenPerParent(parents, children, Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Equal("A", ((Contact)perParent[0][0]).LastName);
        Assert.Equal("B", ((Contact)perParent[1][0]).LastName);
    }

    [Fact]
    public void ChildrenPerParent_WhenNothingPointsAtAParent_GivesItAnEmptyList()
    {
        // Arrange
        List<object> parents = [new Account { Id = IdMocker.GenerateId() }];
        List<object> children = [new Contact { AccountId = IdMocker.GenerateId() }];

        // Act
        List<List<object>> perParent = InverseAlignment.ChildrenPerParent(parents, children, Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Empty(perParent[0]);
    }

    [Fact]
    public void ChildrenPerParent_WhenThereAreFewerChildrenThanParents_PositionFallbackGivesEmpty()
    {
        // Arrange
        List<object> parents = [new Account(), new Account()];
        List<object> children = [new Contact { LastName = "A" }];

        // Act
        List<List<object>> perParent = InverseAlignment.ChildrenPerParent(parents, children, Field.Of<Contact>(x => x.AccountId));

        // Assert
        _ = Assert.Single(perParent[0]);
        Assert.Empty(perParent[1]); // no child at that position
    }
}
