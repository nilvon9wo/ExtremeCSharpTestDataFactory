using global::AutoBogus;
using Net.Nowhereatall.Xfty.AutoBogus;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.AutoBogus.Test;

/// <summary>Proves AutoBogusUnsetFieldFiller - the AutoBogus-backed IUnsetFieldFiller. See UnsetFieldFillerTest (Xfty.Test) for the core contract it relies on.</summary>
public class AutoBogusUnsetFieldFillerTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void Supply_FillsFieldsTheMasterTemplateNeverConfigured_WithRealAutoBogusValues()
    {
        // Arrange
        IAutoFaker faker = AutoFaker.Create();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(new AutoBogusUnsetFieldFiller(faker));

        // Act
        Account result = (Account)provider.Supply();

        // Assert - AccountDataProvider's own Master Template never touches these
        _ = Assert.NotNull(result.NumberOfEmployees);
        _ = Assert.NotNull(result.AnnualRevenue);
        Assert.NotNull(result.Site);
    }

    [Fact]
    public void Supply_NeverOverwritesAFieldTheMasterTemplateDidConfigure()
    {
        // Arrange
        IAutoFaker faker = AutoFaker.Create();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(new AutoBogusUnsetFieldFiller(faker));

        // Act
        Account result = (Account)provider.Supply();

        // Assert - AccountDataProvider's own declared defaults, untouched by AutoBogus
        Assert.StartsWith(AccountDataProvider.DefaultNamePrefix, result.Name);
        Assert.Equal(AccountDataProvider.DefaultIndustry, result.Industry);
    }

    [Fact]
    public void Excluding_LeavesThatOneFieldExactlyAsXftyLeftIt()
    {
        // Arrange
        IAutoFaker faker = AutoFaker.Create();
        AutoBogusUnsetFieldFiller filler = new AutoBogusUnsetFieldFiller(faker)
            .Excluding(Field.Of<Account>(x => x.Site));
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        Account result = (Account)provider.Supply();

        // Assert - excluded field stays null; a sibling unset field still gets filled
        Assert.Null(result.Site);
        _ = Assert.NotNull(result.NumberOfEmployees);
    }

    [Fact]
    public void Excluding_ChainedForEveryNavigationProperty_LeavesAllOfThemUntouched()
    {
        // Arrange - the exact navigation-property triple documented in use/autobogus.md
        IAutoFaker faker = AutoFaker.Create();
        AutoBogusUnsetFieldFiller filler = new AutoBogusUnsetFieldFiller(faker)
            .Excluding(Field.Of<Account>(x => x.Contacts))
            .Excluding(Field.Of<Account>(x => x.Parent))
            .Excluding(Field.Of<Account>(x => x.ChildAccounts));
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        Account result = (Account)provider.Supply();

        // Assert
        Assert.Null(result.Contacts);
        Assert.Null(result.Parent);
        Assert.Null(result.ChildAccounts);
    }

    [Fact]
    public void Supply_ForASelfReferencingUnsetField_NeverThrows()
    {
        // Arrange - Account.Parent is Account itself; AutoBogus self-limits
        // recursion depth rather than throwing, unlike AutoFixture's default
        IAutoFaker faker = AutoFaker.Create();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(new AutoBogusUnsetFieldFiller(faker));

        // Act
        Exception? thrown = Record.Exception(() => provider.Supply());

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void Supply_CombinedWithXftyAutoBogus_FillsAScalarFieldWhileStillGeneratingRelationshipsViaXfty()
    {
        // Arrange - the two features compose: XFTY resolves the required Account
        // relationship; AutoBogus fills whatever scalar fields are left over.
        IAutoFaker faker = XftyAutoBogus.CreateFaker(Lookup());
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetUnsetFieldFiller(new AutoBogusUnsetFieldFiller(faker));

        // Act
        Contact result = (Contact)provider.Supply();

        // Assert
        Assert.NotNull(result.AccountId); // XFTY's own relationship resolution
        _ = Assert.NotNull(result.Birthdate); // ContactDataProvider never configures this
    }
}
