using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Enrichment;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Test.Enrichment;

/// <summary>
/// End-to-end proof that BundleEnricher actually works through the real
/// engine: a generated ancestor / child collection, injected via reflection
/// onto the demo domain's Account.Contacts / Contact.Account navigation
/// properties (added purely so injection has somewhere to land - a plain
/// POCO has no relationship navigation of its own to populate).
/// </summary>
public class EnrichmentIntegrationTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public async Task InjectAll_ForAGeneratedAncestor_PopulatesTheNavigationPropertyOnNewInstances()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required);
        Bundle bundle = await provider.SupplyBundle();

        // Act - InjectAll targets the bundle's primary field, not the ancestor key (which addresses the ancestor sub-bundle itself)
        List<object> enriched = bundle.InjectAll(Field.Of<Contact>(x => x.Id));

        // Assert - the enriched copy carries the populated Account; the original bundle record does not
        Contact enrichedContact = Assert.IsType<Contact>(Assert.Single(enriched));
        Assert.NotNull(enrichedContact.Account);
        Assert.Equal(enrichedContact.AccountId, enrichedContact.Account!.Id);
        Contact original = Assert.IsType<Contact>(Assert.Single(bundle.PrimaryRecords()!));
        Assert.Null(original.Account);
    }

    [Fact]
    public async Task InjectAll_ForAGeneratedChildCollection_PopulatesTheCollectionNavigationProperty()
    {
        // Arrange - downward generation: an Account with three Contact children
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 3);
        Bundle bundle = await provider.SupplyBundle();

        // Act
        List<object> enriched = bundle.InjectAll(Field.Of<Account>(x => x.Id));

        // Assert
        Account enrichedAccount = Assert.IsType<Account>(Assert.Single(enriched));
        Assert.NotNull(enrichedAccount.Contacts);
        Assert.Equal(3, enrichedAccount.Contacts!.Count);
        Assert.All(enrichedAccount.Contacts, contact => Assert.Equal(enrichedAccount.Id, contact.AccountId));
    }

    [Fact]
    public async Task InjectAll_WhenTheGraphHasNothingToInject_Throws()
    {
        // Arrange - no ancestor generated, no children configured
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.None);
        Bundle bundle = await provider.SupplyBundle();

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => bundle.InjectAll(Field.Of<Contact>(x => x.Id)));

        // Assert
        Assert.Contains("has no generated ancestor or child collection", thrown.Message);
    }

    [Fact]
    public async Task Inject_WithInjectValue_ForcesAScalarOntoEveryRow()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetQuantityPerTemplate(2);
        Bundle bundle = await provider.SupplyBundle();
        InjectConfig config = InjectConfig.Nothing().InjectValue(Field.Of<Contact>(x => x.Department), "Sales");

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        // Assert
        Assert.All(enriched.Cast<Contact>(), contact => Assert.Equal("Sales", contact.Department));
    }
}
