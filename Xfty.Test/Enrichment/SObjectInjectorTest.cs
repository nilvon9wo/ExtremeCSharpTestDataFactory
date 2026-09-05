using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>
/// Proves SObjectInjector - the per-row clone + PropertyInfo.SetValue that
/// writes what an init-only property rejects. Pure in-memory, no persistence.
///
/// Not ported: Apex's polymorphic-relationship, Blob, and compound-field
/// cases - this demo domain has no polymorphic lookup, and a reflection-based
/// injector has no Blob-vs-JSON problem to prove in the first place (see
/// SObjectInjector's own doc comment) nor any compound-field concept.
/// </summary>
public class SObjectInjectorTest
{
    [Fact]
    public void Result_WhenAParentIsGrafted_TheRelationshipObjectIsReadableOnEachRow()
    {
        // Arrange
        List<object> contacts = [new Contact { LastName = "Zero" }, new Contact { LastName = "One" }];
        List<object> accounts = [new Account { Name = "Acme" }, new Account { Name = "Globex" }];

        // Act
        List<Contact> enriched = [.. SObjectInjector.Inject(contacts).Relationship(Field.Of<Contact>(nameof(Contact.Account)), accounts).Result().Cast<Contact>()];

        // Assert
        Assert.Equal("Acme", enriched[0].Account!.Name);
        Assert.Equal("Globex", enriched[1].Account!.Name);
    }

    [Fact]
    public void Result_WhenAParentEntryIsNull_LeavesThatRowsRelationshipNull()
    {
        // Arrange
        List<object> contacts = [new Contact { LastName = "Zero" }, new Contact { LastName = "One" }];
        List<object> accounts = [new Account { Name = "Acme" }, null!];

        // Act
        List<Contact> enriched = [.. SObjectInjector.Inject(contacts).Relationship(Field.Of<Contact>(nameof(Contact.Account)), accounts).Result().Cast<Contact>()];

        // Assert
        Assert.Equal("Acme", enriched[0].Account!.Name);
        Assert.Null(enriched[1].Account);
    }

    [Fact]
    public void Result_WhenAChildRelationshipIsGrafted_TheSubqueryIsReadable()
    {
        // Arrange
        List<object> accounts = [new Account { Name = "Parent" }];
        List<List<object>> contactsPerRow = [[new Contact { LastName = "A" }, new Contact { LastName = "B" }]];

        // Act
        List<Account> enriched = [.. SObjectInjector.Inject(accounts).ChildRelationship(Field.Of<Account>(nameof(Account.Contacts)), contactsPerRow).Result().Cast<Account>()];

        // Assert
        Assert.Equal(2, enriched[0].Contacts!.Count);
        Assert.Equal("A", enriched[0].Contacts![0].LastName);
    }

    [Fact]
    public void Result_WhenAChildRowHasNoChildren_TheSubqueryIsEmptyNotNull()
    {
        // Arrange
        List<object> accounts = [new Account { Name = "Childless" }];
        List<List<object>> none = [[]];

        // Act
        List<Account> enriched = [.. SObjectInjector.Inject(accounts).ChildRelationship(Field.Of<Account>(nameof(Account.Contacts)), none).Result().Cast<Account>()];

        // Assert
        Assert.Empty(enriched[0].Contacts!);
    }

    [Fact]
    public void Result_WhenAUniformValueIsSet_EveryRowHasIt()
    {
        // Arrange
        List<object> accounts = [new Account { Name = "A" }, new Account { Name = "B" }];
        const string site = "HQ";

        // Act
        List<Account> enriched = [.. SObjectInjector.Inject(accounts).Value(Field.Of<Account>(nameof(Account.Site)), site).Result().Cast<Account>()];

        // Assert
        Assert.Equal(site, enriched[0].Site);
        Assert.Equal(site, enriched[1].Site);
    }

    [Fact]
    public void Result_WhenPerRowValuesAreSet_EachRowHasItsOwn()
    {
        // Arrange
        List<object> accounts = [new Account { Name = "A" }, new Account { Name = "B" }];
        List<object?> revenues = [100m, 250m];

        // Act
        List<Account> enriched = [.. SObjectInjector.Inject(accounts).ValuePerRow(Field.Of<Account>(nameof(Account.AnnualRevenue)), revenues).Result().Cast<Account>()];

        // Assert
        Assert.Equal(100m, enriched[0].AnnualRevenue);
        Assert.Equal(250m, enriched[1].AnnualRevenue);
    }

    [Fact]
    public void Result_Always_LeavesTheInputRecordsUntouchedAndReturnsNewInstances()
    {
        // Arrange
        Contact original = new() { LastName = "Original" };
        List<object> contacts = [original];

        // Act
        List<Contact> enriched = [.. SObjectInjector.Inject(contacts).Relationship(Field.Of<Contact>(nameof(Contact.Account)), [new Account { Name = "Grafted" }]).Result().Cast<Contact>()];

        // Assert
        Assert.Null(original.Account); // the input record was not mutated
        Assert.NotSame(original, enriched[0]); // the result is a new instance
        Assert.Equal("Grafted", enriched[0].Account!.Name);
    }

    [Fact]
    public void Result_WhenTheRecordListIsEmpty_ReturnsAnEmptyList()
    {
        // Arrange
        List<object> none = [];

        // Act
        List<object> enriched = SObjectInjector.Inject(none).Result();

        // Assert
        Assert.Empty(enriched);
    }

    [Fact]
    public void Inject_WhenRecordsIsNull_Throws()
    {
        // Arrange
        List<object> nothing = null!;

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => SObjectInjector.Inject(nothing));

        // Assert
        Assert.NotNull(thrown);
    }

    [Fact]
    public void Result_WhenAParentListIsMisaligned_Throws()
    {
        // Arrange
        List<object> contacts = [new Contact { LastName = "Zero" }, new Contact { LastName = "One" }];
        List<object> onlyOneAccount = [new Account { Name = "Acme" }];

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => SObjectInjector.Inject(contacts).Relationship(Field.Of<Contact>(nameof(Contact.Account)), onlyOneAccount).Result());

        // Assert - the message names the misaligned graft
        Assert.NotNull(thrown);
        Assert.Contains("Account", thrown.Message);
    }

    [Fact]
    public void Result_WhenPerRowValuesAreMisaligned_Throws()
    {
        // Arrange
        List<object> accounts = [new Account { Name = "A" }, new Account { Name = "B" }];
        List<object?> tooFew = [1m];

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => SObjectInjector.Inject(accounts).ValuePerRow(Field.Of<Account>(nameof(Account.AnnualRevenue)), tooFew).Result());

        // Assert
        Assert.NotNull(thrown);
    }

    [Fact]
    public void Result_WhenAChildAlreadyCarriesItsOwnSubquery_TheNestingSurvivesTheRoundTrip()
    {
        // Arrange - a Contact that already has its Cases populated (from a prior injector pass)
        List<Contact> contactsWithCases = [.. SObjectInjector.Inject([new Contact { LastName = "Parent" }])
            .ChildRelationship(Field.Of<Contact>(nameof(Contact.Cases)), [[new Case { Subject = "grandchild" }]])
            .Result()
            .Cast<Contact>()];
        List<object> accounts = [new Account { Name = "Root" }];

        // Act
        List<Account> enriched = [.. SObjectInjector.Inject(accounts)
            .ChildRelationship(Field.Of<Account>(nameof(Account.Contacts)), [[.. contactsWithCases.Cast<object>()]])
            .Result()
            .Cast<Account>()];

        // Assert - a two-level subquery survives the round-trip
        Assert.Equal("grandchild", enriched[0].Contacts![0].Cases![0].Subject);
    }

    [Fact]
    public void Result_WhenGraftsSpanAllThreeKinds_AppliesEveryOneInOnePass()
    {
        // Arrange
        List<object> contacts = [new Contact { LastName = "Solo" }];
        List<object> accounts = [new Account { Name = "Parent" }];
        List<List<object>> casesPerRow = [[new Case { Subject = "child" }]];
        const string department = "Sales";

        // Act
        List<Contact> enriched = [.. SObjectInjector.Inject(contacts)
            .Relationship(Field.Of<Contact>(nameof(Contact.Account)), accounts)
            .ChildRelationship(Field.Of<Contact>(nameof(Contact.Cases)), casesPerRow)
            .Value(Field.Of<Contact>(nameof(Contact.Department)), department)
            .Result()
            .Cast<Contact>()];

        // Assert
        Assert.Equal("Parent", enriched[0].Account!.Name);
        Assert.Equal("child", enriched[0].Cases![0].Subject);
        Assert.Equal(department, enriched[0].Department);
    }
}
