using System.Reflection;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Test.Engine;

/// <summary>
/// Proves <see cref="DeferredGraph"/> - the read-only view a custom
/// <c>IDeferred</c> walks (docs/extend/custom-value-expressions.md).
/// </summary>
public class DeferredGraphTest
{
    private static readonly PropertyInfo ContactsAccount = Field.Of<Contact>(x => x.AccountId);
    private static readonly PropertyInfo ContactsManager = Field.Of<Contact>(x => x.ReportsToId);

    [Fact]
    public void ChildrenOf_ReturnsOnlyTheRecordsLinkedToThatParentThroughThatField()
    {
        // Arrange - records: [account, child via AccountId, other child via ReportsToId, child via AccountId]
        Account account = new();
        Contact firstChild = new() { LastName = "First" };
        Contact managedOnly = new() { LastName = "Managed" };
        Contact secondChild = new() { LastName = "Second" };
        DeferredGraph graph = new(
            [account, firstChild, managedOnly, secondChild],
            [
                new DepthBatchedInserterParentLink(1, 0, ContactsAccount),
                new DepthBatchedInserterParentLink(2, 0, ContactsManager),
                new DepthBatchedInserterParentLink(3, 0, ContactsAccount),
            ]
        );

        // Act
        List<object> children = graph.ChildrenOf(0, ContactsAccount);

        // Assert
        Assert.Equal([firstChild, secondChild], children);
    }
}