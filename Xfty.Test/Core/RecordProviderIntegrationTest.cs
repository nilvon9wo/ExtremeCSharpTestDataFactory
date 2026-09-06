using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// End-to-end proof that the ported engine actually works together:
/// RecordProvider -&gt; RecordFactory -&gt; AncestorGenerator -&gt; LookupWiring -&gt;
/// PlainValueFiller -&gt; IdMocker, driven by two real IRecordProvider
/// implementations and a required relationship between them - not a unit
/// test of one class in isolation.
/// </summary>
public class RecordProviderIntegrationTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public async Task Supply_ForAContactWithARequiredAccountRelationship_GeneratesBothRecords()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required);

        // Act
        object result = await provider.Supply();

        // Assert
        Contact contact = Assert.IsType<Contact>(result);
        Assert.NotNull(contact.Id);
        Assert.NotNull(contact.AccountId);
        Assert.Equal($"{ContactDataProvider.DefaultLastNamePrefix} 1", contact.LastName);
    }

    [Fact]
    public async Task Supply_WhenInclusivityIsNone_LeavesTheRelationshipUngenerated()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.None);

        // Act
        object result = await provider.Supply();

        // Assert - no ancestor was generated, so there is nothing to wire the lookup to
        Contact contact = Assert.IsType<Contact>(result);
        Assert.Null(contact.AccountId);
    }

    [Fact]
    public async Task SupplyList_WithAQuantity_GeneratesOneAccountPerContact()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetQuantityPerTemplate(3);

        // Act
        List<object> results = await provider.SupplyList();

        // Assert
        List<Contact> contacts = [.. results.Cast<Contact>()];
        Assert.Equal(3, contacts.Count);
        Assert.Equal(3, contacts.Select(contact => contact.AccountId).Distinct().Count());
    }

    [Fact]
    public async Task Supply_WithAnOverrideTemplate_TheOverrideWins()
    {
        // Arrange
        RecordProvider provider = new(new Contact { LastName = "Explicit" }, Lookup());

        // Act
        object result = await provider.Supply();

        // Assert
        Contact contact = Assert.IsType<Contact>(result);
        Assert.Equal("Explicit", contact.LastName);
    }

    [Fact]
    public async Task SupplyBundle_WithChildren_GeneratesTheConfiguredChildCollectionWiredToTheParent()
    {
        // Arrange - downward generation: an Account with three Contact children
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 3);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        List<object> children = bundle.GetChildList<Contact>(x => x.AccountId);
        Account account = Assert.IsType<Account>(bundle.GetList<Account>(x => x.Id)![0]);
        Assert.Equal(3, children.Count);
        Assert.All(children.Cast<Contact>(), contact => Assert.Equal(account.Id, contact.AccountId));
    }

    [Fact]
    public async Task Supply_WhenDepthBatchedWithNowMode_AttemptsRealPersistence()
    {
        // Arrange - depth-batched insert always needs a real persistence layer, which this port doesn't have yet
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Now)
            .SetInclusivity(InsertInclusivity.Required)
            .DepthBatched();

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(provider.Supply);

        // Assert - the depth-batched path was actually engaged, not silently skipped
        Assert.Contains("persistence gateway", thrown.Message);
    }

    [Fact]
    public async Task DeferredInserter_Flush_AfterADeferredSupply_AttemptsRealPersistence()
    {
        // Arrange - DEFERRED mode builds the graph like Never and registers it; Flush() is where real persistence would happen
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Deferred)
            .SetInclusivity(InsertInclusivity.Required);
        _ = await provider.Supply();

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(() => DeferredInserter.Flush());

        // Assert - the registry actually tried to persist, not silently no-op
        Assert.Contains("persistence gateway", thrown.Message);
        DeferredInserter.ResetForTesting(); // the failed Flush() deliberately left the registry non-empty
    }
}
