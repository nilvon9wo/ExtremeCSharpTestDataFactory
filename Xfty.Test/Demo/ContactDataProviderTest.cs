using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;

namespace Net.Nowhereatall.Xfty.Test.Demo;

/// <summary>
/// Proves ContactDataProvider end to end: a generated Contact, its required
/// Account (with the documented defaults), and the inclusivity that controls
/// whether that Account is generated. Uses Mock rather than Now/a real
/// persistence gateway, since the wiring under test is unaffected by whether
/// anything actually gets saved - see PersistenceGatewayTest for the
/// insert-mode proof itself.
/// </summary>
public class ContactDataProviderTest
{
    private static readonly DefaultProviderLookup Lookup = new();
    private const string TestFirstName = "Fred";

    private static RecordProvider ContactProvider(InsertInclusivity inclusivity, InsertMode mode) =>
        new RecordProvider(typeof(Contact), Lookup)
            .SetOverrideTemplate(new Contact { FirstName = TestFirstName })
            .SetInclusivity(inclusivity)
            .SetInsertMode(mode);

    [Fact]
    public void SupplyBundle_AtNoneInclusivity_GeneratesTheContactButNotAnAccount()
    {
        // Arrange
        RecordProvider provider = ContactProvider(InsertInclusivity.None, InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        AssertContactGenerated(bundle);
        Assert.Null(bundle.GetList<Contact>(x => x.AccountId)); // no Account generated at None inclusivity
        Assert.Null(((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId);
    }

    [Fact]
    public void SupplyBundle_AtAllInclusivityInMockMode_GeneratesTheContactAndItsAccountWithTheDocumentedDefaults()
    {
        // Arrange
        RecordProvider provider = ContactProvider(InsertInclusivity.All, InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        AssertContactGenerated(bundle);
        Account generatedAccount = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.NotNull(generatedAccount.Id);
        Assert.Contains(AccountDataProvider.DefaultNamePrefix, generatedAccount.Name);
        Assert.Equal(ContactDataProvider.DefaultAccountDescription, generatedAccount.Description);
        Assert.Equal(generatedAccount.Id, ((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId); // the FK is wired
    }

    [Fact]
    public void SupplyBundle_AtAllInclusivityInMockMode_WiresTheGraphWithoutTouchingADatabase()
    {
        // Arrange
        RecordProvider provider = ContactProvider(InsertInclusivity.All, InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        AssertContactGenerated(bundle);
        Assert.NotNull(((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Id); // the Account got a mock Id
        Assert.Equal(
            ((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Id,
            ((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId); // mock Ids still wire the FK
    }

    // Master Template ------------------------------------

    [Fact]
    public void MasterTemplate_DeclaresARequiredAccountRelationship()
    {
        // Arrange
        ContactDataProvider provider = new();

        // Act
        MasterTemplate template = provider.MasterTemplate;

        // Assert
        Assert.True(template.RequiredRelationshipByField.ContainsKey(Field.Of<Contact>(x => x.AccountId))); // Contact requires an Account
    }

    [Fact]
    public void SupplyList_WithQuantity_GeneratesAUniqueEmailPerRecord()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .SetQuantityPerTemplate(3)
            .SetInsertMode(InsertMode.Mock);

        // Act
        List<object> contacts = provider.SupplyList();

        // Assert
        HashSet<object?> emails = [.. contacts.Cast<Contact>().Select(contact => contact.Email)];
        Assert.Equal(3, emails.Count); // each generated Contact gets its own email
    }

    private static void AssertContactGenerated(Bundle bundle)
    {
        Contact generated = (Contact)bundle.GetList<Contact>(x => x.Id)![0];
        Assert.NotNull(generated.Id);
        Assert.Equal(TestFirstName, generated.FirstName);
        Assert.Contains(ContactDataProvider.DefaultLastNamePrefix, generated.LastName);
    }
}
