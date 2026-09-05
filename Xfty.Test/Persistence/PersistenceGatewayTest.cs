using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Persistence;
using NSubstitute;

namespace Net.Nowhereatall.Xfty.Test.Persistence;

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
    public void Supply_InNowMode_WithAGateway_InsertsThroughItAndKeepsTheAssignedId()
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
        Account result = (Account)provider.Supply();

        // Assert
        Assert.NotNull(result.Id);
        Assert.StartsWith("real-", result.Id);
        gateway.Received(1).Insert(Arg.Is<List<object>>(list => list.Contains(result)), Arg.Any<System.Reflection.PropertyInfo>());
    }

    [Fact]
    public void Supply_InNowMode_WithoutAGateway_StillThrows()
    {
        // Arrange - no SetPersistenceGateway(...) call
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup).SetInsertMode(InsertMode.Now);

        // Act
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() => provider.Supply());

        // Assert
        Assert.Contains("persistence gateway", thrown.Message);
    }

    [Fact]
    public void SupplyBundle_InNowMode_WithAGateway_InsertsTheRequiredParentToo()
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
        Bundle bundle = provider.SupplyBundle();

        // Assert - both the Contact and its required Account were inserted through the gateway
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList(Field.Of<Contact>(x => x.AccountId))![0];
        Assert.NotNull(contact.Id);
        Assert.NotNull(account.Id);
        Assert.Equal(account.Id, contact.AccountId);
        gateway.Received(2).Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>());
    }

    [Fact]
    public void Supply_NowPlusDepthBatched_WithAGateway_ResolvesOneLayerAtATimeThroughTheGateway()
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
        Contact result = (Contact)provider.Supply();

        // Assert - the Account layer landed before the Contact layer, and the FK is real
        Assert.Equal([typeof(Account), typeof(Contact)], insertedLayers);
        Assert.NotNull(result.AccountId);
        Assert.StartsWith("real-", result.AccountId);
    }

    [Fact]
    public void DeferredInserterFlush_WithAGateway_InsertsEverythingRegisteredAndBackFillsIds()
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
        Bundle bundle = new RecordProvider(typeof(Account), Lookup)
            .SetInsertMode(InsertMode.Deferred)
            .SetQuantityPerTemplate(3)
            .SupplyBundle();
        int beforeFlush = DeferredInserter.PendingCount();

        // Act
        DeferredInserter.Flush(gateway);

        // Assert
        Assert.True(beforeFlush >= 3);
        Assert.All(bundle.PrimaryRecords()!.Cast<Account>(), account => Assert.NotNull(account.Id));
        Assert.Equal(0, DeferredInserter.PendingCount());
    }
}
