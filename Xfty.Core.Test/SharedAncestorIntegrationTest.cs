using Net.Nowhereatall.Xfty.Core.Core;
using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Lookup;
using Net.Nowhereatall.Xfty.Core.Relationships;

namespace Net.Nowhereatall.Xfty.Core.Test;

/// <summary>
/// End-to-end proof that SharedAncestor/SharedAncestorResolver actually
/// resolve through the real engine: every child that references a shared
/// ancestor gets the exact same generated (and persisted-or-mocked) record.
///
/// SharedAncestor's registry is process-static, not reset between xUnit
/// tests the way Apex resets statics between test methods (same gap already
/// documented for the unique-value expressions) - each test below uses its
/// own never-reused shared-ancestor name to stay isolated.
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
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(sharedName))
            .SetQuantityPerTemplate(2);

        // Act
        List<object> results = provider.SupplyList();

        // Assert - both contacts point at the very same generated Account Id
        List<string?> accountIds = results.Cast<Contact>().Select(contact => contact.AccountId).Distinct().ToList();
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
}
