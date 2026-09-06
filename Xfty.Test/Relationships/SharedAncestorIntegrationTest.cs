using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Test.Relationships;

/// <summary>
/// End-to-end proof that SharedAncestor/SharedAncestorResolver actually
/// resolve through the real engine: every child that references a shared
/// ancestor gets the exact same generated (and persisted-or-mocked) record.
///
/// SharedAncestor's registry is process-static, not reset between xUnit test
/// methods (same gap already documented for the unique-value expressions) -
/// each test below uses its own never-reused shared-ancestor name to stay
/// isolated.
/// </summary>
public class SharedAncestorIntegrationTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public async Task SupplyList_WithASharedAncestor_PointsEveryChildAtTheSameGeneratedParent()
    {
        // Arrange - two Contacts sharing one Account
        const string sharedName = "shared-ancestor-test-two-contacts";
        _ = SharedAncestor.PutAsTemplate(sharedName, new Account { Name = "ACME HQ" });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(sharedName))
            .SetQuantityPerTemplate(2);

        // Act
        List<object> results = await provider.SupplyList();

        // Assert - both contacts point at the very same generated Account Id
        List<string?> accountIds = [.. results.Cast<Contact>().Select(contact => contact.AccountId).Distinct()];
        _ = Assert.Single(accountIds);
        Assert.NotNull(accountIds[0]);
    }

    [Fact]
    public async Task GetId_AfterResolveNow_ReturnsTheGeneratedId()
    {
        // Arrange
        const string sharedName = "shared-ancestor-test-resolve-now";
        _ = SharedAncestor.PutAsTemplate(sharedName, new Account { Name = "Resolved Up Front" });

        // Act
        _ = await SharedAncestor.Get(sharedName).ResolveNow(Lookup(), InsertMode.Mock);

        // Assert
        Assert.NotNull(SharedAncestor.GetId(sharedName));
    }

    [Fact]
    public void GetId_WhenNeverResolved_Throws()
    {
        // Arrange
        const string sharedName = "shared-ancestor-test-unresolved";
        _ = SharedAncestor.Get(sharedName);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => SharedAncestor.GetId(sharedName));

        // Assert
        Assert.Contains("not resolved yet", thrown.Message);
    }

    [Fact]
    public async Task Supply_WhenTheLookupRegistersASharedAncestorDefault_ResolvesItWithoutBeingPutExplicitly()
    {
        // Arrange - the lookup itself supplies the default template (ISharedAncestorDefaults), not the test
        const string sharedName = "shared-ancestor-test-lookup-default";
        IProviderLookup lookup = ProviderLookups.Of(
            new Dictionary<ILookupKey, IRecordProvider>
            {
                [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
                [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
            },
            new Dictionary<string, object> { [sharedName] = new Account { Name = "Lookup-Default HQ" } });
        RecordProvider provider = new RecordProvider(typeof(Contact), lookup)
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(sharedName));

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.NotNull(result.AccountId);
        Assert.Equal(result.AccountId, SharedAncestor.GetId(sharedName));
    }

    [Fact]
    public async Task SupplyBundle_MockWithExcludePrimaryIds_WithASharedAncestor_MockResolvesItWithoutAGateway()
    {
        // Arrange - a shared ancestor resolves eagerly under Mock the same way it always did;
        // ExcludePrimaryIds only changes what happens to this call's own primary
        const string sharedName = "shared-ancestor-test-mock-exclude-primary-ids";
        _ = SharedAncestor.PutAsTemplate(sharedName, new Account { Name = "Mock Exclude Primary Ids HQ" });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .ExcludePrimaryIds()
            .SetInclusivity(InsertInclusivity.Required)
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(sharedName));

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - the shared Account resolved with a mock Id; the Contact primary stays un-Id'd
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Assert.Null(contact.Id);
        Assert.NotNull(contact.AccountId);
        Assert.Equal(SharedAncestor.GetId(sharedName), contact.AccountId);
    }
}
