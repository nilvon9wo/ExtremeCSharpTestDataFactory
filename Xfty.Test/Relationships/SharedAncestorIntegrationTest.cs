using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;

namespace Net.Nowhereatall.Xfty.Test.Relationships;

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
    public void SupplyList_WithASharedAncestor_PointsEveryChildAtTheSameGeneratedParent()
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
        List<object> results = provider.SupplyList();

        // Assert - both contacts point at the very same generated Account Id
        List<string?> accountIds = [.. results.Cast<Contact>().Select(contact => contact.AccountId).Distinct()];
        _ = Assert.Single(accountIds);
        Assert.NotNull(accountIds[0]);
    }

    [Fact]
    public void GetId_AfterResolveNow_ReturnsTheGeneratedId()
    {
        // Arrange
        const string sharedName = "shared-ancestor-test-resolve-now";
        _ = SharedAncestor.PutAsTemplate(sharedName, new Account { Name = "Resolved Up Front" });

        // Act
        _ = SharedAncestor.Get(sharedName).ResolveNow(Lookup(), InsertMode.Mock);

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
    public void Supply_WhenTheLookupRegistersASharedAncestorDefault_ResolvesItWithoutBeingPutExplicitly()
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
        Contact result = Assert.IsType<Contact>(provider.Supply());

        // Assert
        Assert.NotNull(result.AccountId);
        Assert.Equal(result.AccountId, SharedAncestor.GetId(sharedName));
    }

    [Fact]
    public void SupplyBundle_InMockRelatedOnlyMode_WithASharedAncestor_MockResolvesItWithoutAGateway()
    {
        // Arrange - MockRelatedOnly's shared-ancestor resolution is eager too, but as Mock, not Now
        const string sharedName = "shared-ancestor-test-mock-related-only";
        _ = SharedAncestor.PutAsTemplate(sharedName, new Account { Name = "Mock Related Only HQ" });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.MockRelatedOnly)
            .SetInclusivity(InsertInclusivity.Required)
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(sharedName));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - the shared Account resolved with a mock Id; the Contact primary stays un-Id'd
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Assert.Null(contact.Id);
        Assert.NotNull(contact.AccountId);
        Assert.Equal(SharedAncestor.GetId(sharedName), contact.AccountId);
    }
}
