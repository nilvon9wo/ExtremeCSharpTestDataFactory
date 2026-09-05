using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Lookup;

namespace Net.Nowhereatall.Xfty.Core.Test;

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
    public void Supply_ForAContactWithARequiredAccountRelationship_GeneratesBothRecords()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required);

        // Act
        object result = provider.Supply();

        // Assert
        Contact contact = Assert.IsType<Contact>(result);
        Assert.NotNull(contact.Id);
        Assert.NotNull(contact.AccountId);
        Assert.Equal("Contact 1", contact.LastName);
    }

    [Fact]
    public void Supply_WhenInclusivityIsNone_LeavesTheRelationshipUngenerated()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.None);

        // Act
        object result = provider.Supply();

        // Assert - no ancestor was generated, so there is nothing to wire the lookup to
        Contact contact = Assert.IsType<Contact>(result);
        Assert.Null(contact.AccountId);
    }

    [Fact]
    public void SupplyList_WithAQuantity_GeneratesOneAccountPerContact()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetQuantityPerTemplate(3);

        // Act
        List<object> results = provider.SupplyList();

        // Assert
        List<Contact> contacts = results.Cast<Contact>().ToList();
        Assert.Equal(3, contacts.Count);
        Assert.Equal(3, contacts.Select(contact => contact.AccountId).Distinct().Count());
    }

    [Fact]
    public void Supply_WithAnOverrideTemplate_TheOverrideWins()
    {
        // Arrange
        RecordProvider provider = new(new Contact { LastName = "Explicit" }, Lookup());

        // Act
        object result = provider.Supply();

        // Assert
        Contact contact = Assert.IsType<Contact>(result);
        Assert.Equal("Explicit", contact.LastName);
    }
}
