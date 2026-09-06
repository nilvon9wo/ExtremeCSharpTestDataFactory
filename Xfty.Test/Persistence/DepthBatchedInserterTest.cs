using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Test.Persistence;

/// <summary>
/// Proves DepthBatchedInserter.ResolveAll - one pass per dependency layer, of
/// any mix of record types, either assigning mock Ids or (with a real
/// persistence gateway) inserting. Every wiring/layering/cycle scenario below
/// uses ResolveAll(..., InsertMode.Mock), the same underlying algorithm a
/// real Now insert runs; the gateway-backed InsertAll path itself is proven
/// in PersistenceGatewayTest.
///
/// Best-effort org-wide seeding is explicitly out of scope for this port
/// (see docs/contribute/porting-history.md) and is not covered here.
/// </summary>
public class DepthBatchedInserterTest
{
    [Fact]
    public Task ResolveAll_ForNullLinksAndEmptyRecords_DoesNothing() =>
        DepthBatchedInserter.ResolveAll([], null, InsertMode.Mock); // no throw

    [Fact]
    public async Task ResolveAll_ForIndependentRecords_AssignsEveryOneAnId()
    {
        // Arrange
        List<object> records = [new Account { Name = "A" }, new Account { Name = "B" }, new Account { Name = "C" }];

        // Act
        await DepthBatchedInserter.ResolveAll(records, null, InsertMode.Mock);

        // Assert
        Assert.NotNull(((Account)records[2]).Id);
    }

    [Fact]
    public async Task ResolveAll_ForAParentAndChild_ResolvesTheParentFirstAndPointsTheLookup()
    {
        // Arrange
        Account parent = new() { Name = "Parent Co" };
        Contact child = new() { LastName = "Child" };
        List<object> records = [child, parent];

        // Act
        await DepthBatchedInserter.ResolveAll(records, [Link(0, 1, Field.Of<Contact>(x => x.AccountId))], InsertMode.Mock);

        // Assert
        Assert.Equal(parent.Id, ((Contact)records[0]).AccountId);
    }

    [Fact]
    public async Task ResolveAll_ForParentsOfDifferentTypes_ResolvesThemAtTheSameLayer()
    {
        // Arrange - a Case needs both an Account (WhatId-equivalent) and a Contact (WhoId-equivalent)
        Account account = new() { Name = "What Co" };
        Contact contact = new() { LastName = "Who" };
        Case supportCase = new() { Subject = "Call" };
        List<object> records = [supportCase, account, contact];

        // Act
        await DepthBatchedInserter.ResolveAll(
            records,
            [Link(0, 1, Field.Of<Case>(x => x.AccountId)), Link(0, 2, Field.Of<Case>(x => x.ContactId))],
            InsertMode.Mock);

        // Assert - both parents at layer 0, the Case alone at layer 1
        Assert.Equal(account.Id, ((Case)records[0]).AccountId);
        Assert.Equal(contact.Id, ((Case)records[0]).ContactId);
    }

    [Fact]
    public async Task ResolveAll_ForAChain_ResolvesOneLayerAtATime()
    {
        // Arrange
        Account gen1 = new() { Name = "Gen 1" };
        Contact gen2 = new() { LastName = "Gen 2" };
        Contact gen3 = new() { LastName = "Gen 3" };
        List<object> records = [gen1, gen2, gen3];

        // Act
        await DepthBatchedInserter.ResolveAll(
            records,
            [Link(1, 0, Field.Of<Contact>(x => x.AccountId)), Link(2, 1, Field.Of<Contact>(x => x.ReportsToId))],
            InsertMode.Mock);

        // Assert
        Assert.Equal(gen1.Id, gen2.AccountId);
        Assert.Equal(gen2.Id, gen3.ReportsToId);
    }

    [Fact]
    public async Task ResolveAll_ForOneParentSharedByTwoChildren_ResolvesTheParentOnce()
    {
        // Arrange
        Account parent = new() { Name = "Shared" };
        Contact first = new() { LastName = "First" };
        Contact second = new() { LastName = "Second" };
        List<object> records = [parent, first, second];

        // Act
        await DepthBatchedInserter.ResolveAll(
            records,
            [Link(1, 0, Field.Of<Contact>(x => x.AccountId)), Link(2, 0, Field.Of<Contact>(x => x.AccountId))],
            InsertMode.Mock);

        // Assert
        Assert.Equal(parent.Id, first.AccountId);
        Assert.Equal(parent.Id, second.AccountId);
    }

    [Fact]
    public Task ResolveAll_WhenTwoRecordsReferenceEachOther_Throws() =>
        AssertCyclic(
            [new Account { Name = "A" }, new Account { Name = "B" }],
            [Link(0, 1, Field.Of<Account>(x => x.ParentId)), Link(1, 0, Field.Of<Account>(x => x.ParentId))]);

    [Fact]
    public Task ResolveAll_WhenARecordReferencesItself_Throws() =>
        AssertCyclic([new Account { Name = "Loop" }], [Link(0, 0, Field.Of<Account>(x => x.ParentId))]);

    [Fact]
    public async Task InsertAll_AlwaysResolvesAsNow_WhichThisPortCannotPersist()
    {
        // Arrange
        List<object> records = [new Account { Name = "A" }];

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(() => DepthBatchedInserter.InsertAll(records, null));

        // Assert
        Assert.Contains("persistence gateway", thrown.Message);
    }

    // Runners + helpers -------------------------------------

    private static async Task AssertCyclic(List<object> records, List<DepthBatchedInserterParentLink> parentLinks)
    {
        // Act
        CyclicGraphException thrown = await Assert.ThrowsAsync<CyclicGraphException>(() => DepthBatchedInserter.ResolveAll(records, parentLinks, InsertMode.Mock));

        // Assert - a cyclic graph must be rejected
        Assert.Contains("cycle", thrown.Message);
    }

    private static DepthBatchedInserterParentLink Link(int childIndex, int parentIndex, PropertyInfo field) => new(childIndex, parentIndex, field);
}
