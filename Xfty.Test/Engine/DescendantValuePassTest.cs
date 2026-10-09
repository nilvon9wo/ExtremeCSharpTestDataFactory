using System.Reflection;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Engine;

/// <summary>
/// Proves <see cref="DescendantValuePass"/> - filling each pending up-flow
/// value from the flattened graph, but never over a value the record already
/// carries (an override template's, say).
/// </summary>
public class DescendantValuePassTest
{
    private static readonly PropertyInfo AccountSite = Field.Of<Account>(x => x.Site);
    private static readonly PropertyInfo ContactsAccount = Field.Of<Contact>(x => x.AccountId);

    [Fact]
    public void Complete_WhenTheFieldIsUnset_FillsItFromTheChild()
    {
        // Arrange
        Account account = new();
        DescendantValuePass pass = PassOver(account);

        // Act
        pass.Complete();

        // Assert
        Assert.Equal("Engineering", account.Site);
    }

    [Fact]
    public void Complete_WhenTheFieldIsAlreadySet_LeavesItAlone()
    {
        // Arrange
        Account account = new() { Site = "Preset" };
        DescendantValuePass pass = PassOver(account);

        // Act
        pass.Complete();

        // Assert
        Assert.Equal("Preset", account.Site);
    }

    private static DescendantValuePass PassOver(Account account) =>
        new(
            [account, new Contact { Department = "Engineering" }],
            [new DepthBatchedInserterParentLink(1, 0, ContactsAccount)],
            [
                new PendingDeferredValue(
                    0,
                    AccountSite,
                    CopyFromDescendantExpression.From<Contact>(x => x.AccountId, x => x.Department)
                ),
            ]
        );
}