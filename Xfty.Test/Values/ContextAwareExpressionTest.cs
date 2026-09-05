using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Values;

/// <summary>
/// Proves context-aware value generation driven end to end through
/// RecordProvider.Supply() - the second value pass RecordFactory runs,
/// CopyFromSiblingExpression, CopyFromAncestorExpression, and a custom
/// IContextAwareExpression. Complements CopyFromSiblingExpressionTest /
/// CopyFromAncestorExpressionTest, which exercise the same expressions by
/// building GenerationContext directly - these instead prove them wired
/// through Put(...)/Supply(), where sibling ordering and ancestor generation
/// actually come from the engine.
/// </summary>
public class ContextAwareExpressionTest
{
    private static readonly DefaultProviderLookup DefaultLookup = new();

    private static RecordProvider AccountProvider() =>
        new RecordProvider(typeof(Account), DefaultLookup).SetInsertMode(InsertMode.Mock);

    private static RecordProvider ContactProvider() =>
        new RecordProvider(typeof(Contact), DefaultLookup).SetInsertMode(InsertMode.Mock);

    // CopyFromSiblingExpression ----------------------------

    [Fact]
    public void Supply_ForACopyFromSibling_TakesTheSiblingsPlainValue()
    {
        // Arrange
        RecordProvider provider = AccountProvider()
            .Put<Account>(x => x.ShippingCity, "Berlin")
            .Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCity));

        // Act
        Account result = (Account)provider.Supply();

        // Assert
        Assert.Equal("Berlin", result.BillingCity);
    }

    [Fact]
    public void Supply_ForACopyFromSibling_SeesAnEarlierContextAwareSibling()
    {
        // Arrange - ShippingCity (plain) -> BillingCity (reads ShippingCity) -> BillingStreet (reads BillingCity)
        RecordProvider provider = AccountProvider()
            .Put<Account>(x => x.ShippingCity, "Munich")
            .Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCity))
            .Put<Account>(x => x.BillingStreet, CopyFromSiblingExpression.From<Account>(x => x.BillingCity));

        // Act
        Account result = (Account)provider.Supply();

        // Assert
        Assert.Equal("Munich", result.BillingStreet);
    }

    [Fact]
    public void Supply_ForACopyFromSibling_DoesNotOverrideAValueTheOverrideTemplateSupplied()
    {
        // Arrange
        RecordProvider provider = AccountProvider()
            .Put<Account>(x => x.ShippingCity, "Hamburg")
            .Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCity))
            .SetOverrideTemplate(new Account { BillingCity = "Explicit" });

        // Act
        Account result = (Account)provider.Supply();

        // Assert - the override template still wins
        Assert.Equal("Explicit", result.BillingCity);
    }

    [Fact]
    public void Supply_ForACopyFromSibling_WhenTheSiblingItReadsIsPutAfterIt_Throws()
    {
        // Arrange - Description (reader) is put before Site (a context-aware value it reads)
        RecordProvider provider = AccountProvider()
            .Put<Account>(x => x.Description, CopyFromSiblingExpression.From<Account>(x => x.Site))
            .Put<Account>(x => x.Site, CopyFromSiblingExpression.From<Account>(x => x.AccountNumber))
            .Put<Account>(x => x.AccountNumber, "seed");

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => provider.Supply());

        // Assert
        Assert.Contains("Site", thrown.Message);
        Assert.Contains("has not been generated yet", thrown.Message);
        Assert.Contains("before", thrown.Message);
    }

    [Fact]
    public void Supply_ForACopyFromSibling_WhenTwoSiblingsReadEachOther_Throws()
    {
        // Arrange
        RecordProvider provider = AccountProvider()
            .Put<Account>(x => x.Description, CopyFromSiblingExpression.From<Account>(x => x.Site))
            .Put<Account>(x => x.Site, CopyFromSiblingExpression.From<Account>(x => x.Description));

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => provider.Supply());

        // Assert
        Assert.Contains("has not been generated yet", thrown.Message);
    }

    // CopyFromAncestorExpression --------------------------

    [Fact]
    public void Supply_ForACopyFromAncestor_TakesAFieldFromTheGeneratedParent()
    {
        // Arrange
        RecordProvider provider = ContactProvider()
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account { Name = "Wired Parent" }))
            .Put<Contact>(x => x.Department, CopyFromAncestorExpression.From<Contact, Account>(x => x.AccountId, x => x.Name))
            .SetInclusivity(InsertInclusivity.Required);

        // Act
        Contact result = (Contact)provider.Supply();

        // Assert
        Assert.Equal("Wired Parent", result.Department);
    }

    [Fact]
    public void Supply_ForACopyFromAncestor_WhenTheRelationshipWasNotGenerated_IsNull()
    {
        // Arrange
        RecordProvider provider = ContactProvider()
            .RemoveFromMasterTemplate<Contact>(x => x.AccountId)
            .Put<Contact>(x => x.Department, CopyFromAncestorExpression.From<Contact, Account>(x => x.AccountId, x => x.Name))
            .SetInclusivity(InsertInclusivity.None);

        // Act
        Contact result = (Contact)provider.Supply();

        // Assert - no ancestor generated -> null
        Assert.Null(result.Department);
    }

    [Fact]
    public void SupplyList_ForACopyFromAncestor_WithQuantity_AppliesPerRow()
    {
        // Arrange - the bundled Account Provider gives each generated Account an incrementing Name
        RecordProvider provider = ContactProvider()
            .Put<Contact>(x => x.Department, CopyFromAncestorExpression.From<Contact, Account>(x => x.AccountId, x => x.Name))
            .SetQuantityPerTemplate(3)
            .SetInclusivity(InsertInclusivity.Required);

        // Act
        List<object> results = provider.SupplyList();

        // Assert
        HashSet<object?> departments = [.. results.Cast<Contact>().Select(contact => contact.Department)];
        Assert.Equal(3, departments.Count); // each Contact copied its own row Account name
    }

    [Fact]
    public void Supply_ForACopyFromAncestor_FollowsAMultiHopPath()
    {
        // Arrange - Contact -> Account -> Owner(User); copy the generated Owner's LastName onto the Contact
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
            [LookupKey.Get(typeof(Account))] = new AccountWithOwnerProvider(),
            [LookupKey.Get(typeof(User))] = new LeafUserProvider(),
        });
        RecordProvider provider = new RecordProvider(typeof(Contact), lookup)
            .Put<Contact>(x => x.Department, new CopyFromAncestorExpression([
                Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId), Field.Of<User>(x => x.LastName),
            ]))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = (Contact)provider.Supply();

        // Assert
        Assert.NotNull(result.Department); // the Account Owner's LastName was copied two hops up
    }

    // A custom context-aware expression -----------------------

    [Fact]
    public void Supply_ForACustomContextAwareExpression_CanDeriveFromASibling()
    {
        // Arrange
        RecordProvider provider = ContactProvider()
            .Put<Contact>(x => x.Birthdate, new DateTime(2010, 1, 1))
            .Put<Contact>(x => x.Department, new IsMinorFlag(Field.Of<Contact>(x => x.Birthdate)));

        // Act
        Contact result = (Contact)provider.Supply();

        // Assert
        Assert.Equal("MINOR", result.Department);
    }

    [Fact]
    public void SupplyList_ForACustomContextAwareExpression_SeesTheSiblingPrimaryRecordsInBundleSoFar()
    {
        // Arrange
        RecordProvider provider = AccountProvider()
            .Put<Account>(x => x.Description, new SiblingCountLabel())
            .SetQuantityPerTemplate(3);

        // Act
        List<object> accounts = provider.SupplyList();

        // Assert
        HashSet<object?> labels = [.. accounts.Cast<Account>().Select(account => account.Description)];
        Assert.Equal(new HashSet<object?> { "1 of 3", "2 of 3", "3 of 3" }, labels); // each row sees all three sibling primaries and its own rowIndex
    }
}

/// <summary>An Account whose Owner is generated, so multi-hop tests have a second level.</summary>
file sealed class AccountWithOwnerProvider()
    : SimpleRecordProvider<Account>(
        new MasterTemplate<Account>(x => x.Id)
            .Put(x => x.Name, new IncrementingStringExpression("Acct"))
            .PutRequired(x => x.OwnerId, new DefaultRelationship(new User())));

file sealed class LeafUserProvider()
    : SimpleRecordProvider<User>(
        new MasterTemplate<User>(x => x.Id)
            .Put(x => x.LastName, new IncrementingStringExpression("User")));

/// <summary>Derives a MINOR / ADULT flag from a Birthdate sibling - the kind of logic XFTY leaves to consumers.</summary>
file sealed class IsMinorFlag(System.Reflection.PropertyInfo birthdateField) : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        DateTime? birthdate = (DateTime?)birthdateField.GetValue(context.RecordBeingBuilt);
        return birthdate is not null && birthdate.Value.AddYears(18) > DateTime.Today ? "MINOR" : "ADULT";
    }
}

/// <summary>Reads the whole batch of sibling primary records out of BundleSoFar.</summary>
file sealed class SiblingCountLabel : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        int siblingCount = context.BundleSoFar!.GetList<Account>(x => x.Id)!.Count;
        return $"{context.RowIndex + 1} of {siblingCount}";
    }
}
