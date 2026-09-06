using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Persistence;
using NSubstitute;

namespace Net.NowhereAtAll.Xfty.Test.Persistence;

/// <summary>
/// Proves InsertMode.Now and .DepthBatched() actually work end to end once a
/// persistence gateway is configured - mocking out the gateway itself (via
/// NSubstitute), not the record generation. This is the unit-test side of
/// proving Now works with real persistence; the integration-test side
/// (a real database behind the same interface) lives in
/// Xfty.EntityFrameworkCore.Test.
/// </summary>
public class PersistenceGatewayTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public async Task Supply_InNowMode_WithAGateway_InsertsThroughItAndKeepsTheAssignedId()
    {
        // Arrange - the gateway assigns an Id the way a real database would
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        gateway.When(g => g.Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>()))
            .Do(call =>
            {
                List<object> records = call.ArgAt<List<object>>(0);
                System.Reflection.PropertyInfo idField = call.ArgAt<System.Reflection.PropertyInfo>(1);
                records.ForEach(record => idField.SetValue(record, $"real-{Guid.NewGuid()}"));
            });
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(gateway);

        // Act
        Account result = (Account)await provider.Supply();

        // Assert
        Assert.NotNull(result.Id);
        Assert.StartsWith("real-", result.Id);
        _ = gateway.Received(1).Insert(Arg.Is<List<object>>(list => list.Contains(result)), Arg.Any<System.Reflection.PropertyInfo>());
    }

    [Fact]
    public async Task Supply_InNowMode_WithoutAGateway_StillThrows()
    {
        // Arrange - no SetPersistenceGateway(...) call
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup).SetInsertMode(InsertMode.Now);

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(provider.Supply);

        // Assert
        Assert.Contains("persistence gateway", thrown.Message);
    }

    [Fact]
    public async Task SupplyBundle_InNowMode_WithAGateway_InsertsTheRequiredParentToo()
    {
        // Arrange
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        gateway.When(g => g.Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>()))
            .Do(call =>
            {
                List<object> records = call.ArgAt<List<object>>(0);
                System.Reflection.PropertyInfo idField = call.ArgAt<System.Reflection.PropertyInfo>(1);
                records.ForEach(record => idField.SetValue(record, $"real-{Guid.NewGuid()}"));
            });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(gateway);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - both the Contact and its required Account were inserted through the gateway
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.NotNull(contact.Id);
        Assert.NotNull(account.Id);
        Assert.Equal(account.Id, contact.AccountId);
        _ = gateway.Received(2).Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>());
    }

    [Fact]
    public async Task SupplyBundle_NowWithExcludePrimaryIds_WithAGateway_InsertsTheAncestorButLeavesThePrimaryUnId()
    {
        // Arrange - ExcludePrimaryIds relates a not-yet-inserted Contact to a real, persisted Account
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        gateway.When(g => g.Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>()))
            .Do(call =>
            {
                List<object> records = call.ArgAt<List<object>>(0);
                System.Reflection.PropertyInfo idField = call.ArgAt<System.Reflection.PropertyInfo>(1);
                records.ForEach(record => idField.SetValue(record, $"real-{Guid.NewGuid()}"));
            });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .ExcludePrimaryIds()
            .SetPersistenceGateway(gateway);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - the Account is genuinely inserted; the Contact primary is left for the caller
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Null(contact.Id);
        Assert.NotNull(account.Id);
        Assert.Equal(account.Id, contact.AccountId);
        _ = gateway.Received(1).Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>());
    }

    [Fact]
    public async Task Supply_NowWithExcludePrimaryIds_WithoutAGateway_ThrowsWhenAnAncestorNeedsGenerating()
    {
        // Arrange - Contact's required Account ancestor still needs a real insert, same as bare Now -
        // ExcludePrimaryIds only changes what happens to the primary, never how an ancestor is persisted
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .ExcludePrimaryIds();

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(provider.Supply);

        // Assert
        Assert.Contains("persistence gateway", thrown.Message);
    }

    [Fact]
    public async Task SupplyBundle_MockWithExcludePrimaryIds_MockIdsTheAncestorButLeavesThePrimaryUnId()
    {
        // Arrange - Mock + ExcludePrimaryIds is the offline shape: same "leave the primary
        // un-Id'd" outcome, but the ancestor only needs a mock Id, not a real gateway
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .ExcludePrimaryIds();

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - the Account gets a mock Id; the Contact primary is left for the caller
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Null(contact.Id);
        Assert.NotNull(account.Id);
        Assert.Equal(account.Id, contact.AccountId);
    }

    [Fact]
    public async Task Supply_ExcludePrimaryIdsThenIncludePrimaryIds_PersistsThePrimaryNormally()
    {
        // Arrange - IncludePrimaryIds() undoes ExcludePrimaryIds(), last call wins - useful for a
        // helper that decides dynamically, or just for spelling out the default explicitly
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup)
            .SetInsertMode(InsertMode.Mock)
            .ExcludePrimaryIds()
            .IncludePrimaryIds();

        // Act
        Account result = (Account)await provider.Supply();

        // Assert
        Assert.NotNull(result.Id);
    }

    [Fact]
    public async Task Supply_MockWithExcludePrimaryIds_WithoutAGateway_NeverThrows()
    {
        // Arrange - Mock never needs a gateway, with or without ExcludePrimaryIds
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .ExcludePrimaryIds();

        // Act
        Exception? thrown = await Record.ExceptionAsync(provider.Supply);

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public async Task Flush_AfterDeferredWithExcludePrimaryIds_InsertsEverythingExceptTheExcludedPrimary()
    {
        // Arrange - the capability RelatedOnly/MockRelatedOnly could never express: a whole 10-level-deep
        // ancestor tree (Account here stands in for one) built efficiently under Deferred, flushed for
        // real, while the primary that relates to it stays un-Id'd the entire time
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        int insertCalls = 0;
        gateway.When(g => g.Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>()))
            .Do(call =>
            {
                insertCalls++;
                List<object> records = call.ArgAt<List<object>>(0);
                System.Reflection.PropertyInfo idField = call.ArgAt<System.Reflection.PropertyInfo>(1);
                records.ForEach(record => idField.SetValue(record, $"real-{Guid.NewGuid()}"));
            });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Deferred)
            .ExcludePrimaryIds();

        // Act
        Bundle bundle = await provider.SupplyBundle();
        await DeferredInserter.Flush(gateway);

        // Assert - the Account is genuinely inserted (proving DEFERRED's own efficient batching engaged
        // at all); the Contact primary this call itself produced is never given an Id, even after flush
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Null(contact.Id);
        Assert.NotNull(account.Id);
        Assert.Equal(account.Id, contact.AccountId);
        Assert.Equal(1, insertCalls);
    }

    [Fact]
    public async Task Supply_NowPlusDepthBatched_WithAGateway_ResolvesOneLayerAtATimeThroughTheGateway()
    {
        // Arrange - a Contact requiring an Account: depth-batched should insert the Account layer,
        // then the Contact layer, as two separate gateway calls, parent Id already wired by the second.
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        List<Type> insertedLayers = [];
        gateway.When(g => g.Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>()))
            .Do(call =>
            {
                List<object> records = call.ArgAt<List<object>>(0);
                System.Reflection.PropertyInfo idField = call.ArgAt<System.Reflection.PropertyInfo>(1);
                insertedLayers.Add(records[0].GetType());
                records.ForEach(record => idField.SetValue(record, $"real-{Guid.NewGuid()}"));
            });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(gateway)
            .DepthBatched();

        // Act
        Contact result = (Contact)await provider.Supply();

        // Assert - the Account layer landed before the Contact layer, and the FK is real
        Assert.Equal([typeof(Account), typeof(Contact)], insertedLayers);
        Assert.NotNull(result.AccountId);
        Assert.StartsWith("real-", result.AccountId);
    }

    [Fact]
    public async Task DeferredInserterFlush_WithAGateway_InsertsEverythingRegisteredAndBackFillsIds()
    {
        // Arrange
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        gateway.When(g => g.Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>()))
            .Do(call =>
            {
                List<object> records = call.ArgAt<List<object>>(0);
                System.Reflection.PropertyInfo idField = call.ArgAt<System.Reflection.PropertyInfo>(1);
                records.ForEach(record => idField.SetValue(record, $"real-{Guid.NewGuid()}"));
            });
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup)
            .SetInsertMode(InsertMode.Deferred)
            .SetQuantityPerTemplate(3)
            .SupplyBundle();
        int beforeFlush = DeferredInserter.PendingCount();

        // Act
        await DeferredInserter.Flush(gateway);

        // Assert
        Assert.True(beforeFlush >= 3);
        Assert.All(bundle.PrimaryRecords()!.Cast<Account>(), account => Assert.NotNull(account.Id));
        Assert.Equal(0, DeferredInserter.PendingCount());
    }
}
