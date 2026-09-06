using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>
/// End-to-end generation scenarios through RecordProvider: request a Contact
/// and get a valid Account graph too, and Mock mode not touching a database.
/// Fine-grained API guards live in RecordProviderApiTest.
///
/// Mock mode wires the exact same graph shape/FKs a real insert would; that's
/// what is proven here. Now against a real persistence gateway (and the
/// NotSupportedException it throws with none configured) is proven in
/// PersistenceGatewayTest and RecordProviderIntegrationTest.
/// </summary>
public class RecordProviderScenarioTest
{
    private const string TestContactFirstName = "Fred";

    private static DefaultProviderLookup Lookup() => new();

    [Fact]
    public async Task SupplyBundle_WithRequiredInclusivity_GeneratesTheContactAndItsAccount()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetOverrideTemplateList(ContactTemplateList())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        AssertContactGenerated(bundle);
        Assert.NotNull(bundle.GetList<Contact>(x => x.AccountId));
        Assert.NotNull(bundle.GetBundle<Contact>(x => x.AccountId));
        Assert.NotNull(((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId); // the FK is wired
    }

    [Fact]
    public async Task SupplyBundle_InMockMode_WiresTheGraphWithoutTouchingADatabase()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetOverrideTemplateList(ContactTemplateList())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.All);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        AssertContactGenerated(bundle);
        AssertAccountGenerated(bundle);
        Assert.Equal(
            ((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId,
            ((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Id); // mock Ids still wire the FK
    }

    [Fact]
    public async Task Supply_WithNoInclusivity_GeneratesOnlyTheContact()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetOverrideTemplateList(ContactTemplateList())
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        AssertContactGenerated(result);
        Assert.Null(result.AccountId); // no inclusivity -> no Account
    }

    [Fact]
    public async Task Supply_WithOnlyAType_AppliesTheMasterTemplateDefaults()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup()).SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.Contains(ContactDataProvider.DefaultFirstNamePrefix, result.FirstName);
        Assert.Contains(ContactDataProvider.DefaultLastNamePrefix, result.LastName);
    }

    // Helpers -----------------------------------------------------

    private static List<object> ContactTemplateList() => [new Contact { FirstName = TestContactFirstName }];

    private static void AssertAccountGenerated(Bundle bundle)
    {
        Assert.NotNull(bundle.GetBundle<Contact>(x => x.AccountId));
        List<object>? accounts = bundle.GetList<Contact>(x => x.AccountId);
        Assert.NotNull(accounts);

        Account generatedAccount = (Account)accounts![0];
        Assert.NotNull(generatedAccount.Id);
        Assert.Contains(AccountDataProvider.DefaultNamePrefix, generatedAccount.Name);
        Assert.Equal(ContactDataProvider.DefaultAccountDescription, generatedAccount.Description);
    }

    private static void AssertContactGenerated(Bundle bundle)
    {
        List<object>? contacts = bundle.GetList<Contact>(x => x.Id);
        Assert.NotNull(contacts);
        AssertContactGenerated((Contact)contacts![0]);
    }

    private static void AssertContactGenerated(Contact generatedContact)
    {
        Assert.NotNull(generatedContact.Id);
        Assert.Equal(TestContactFirstName, generatedContact.FirstName);
        Assert.Contains(ContactDataProvider.DefaultLastNamePrefix, generatedContact.LastName);
    }
}
