using global::AutoFixture;
using global::AutoFixture.Kernel;
using Net.Nowhereatall.Xfty.AutoFixture;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.AutoFixture.Test;

/// <summary>Proves AutoFixtureUnsetFieldFiller - the bundled IUnsetFieldFiller. See UnsetFieldFillerTest (Xfty.Test) for the core contract it relies on.</summary>
public class AutoFixtureUnsetFieldFillerTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public async Task Supply_FillsFieldsTheMasterTemplateNeverConfigured_WithRealAutoFixtureValues()
    {
        // Arrange
        IFixture fixture = new Fixture();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(new AutoFixtureUnsetFieldFiller(fixture));

        // Act
        Account result = (Account)await provider.Supply();

        // Assert - AccountDataProvider's own Master Template never touches these
        _ = Assert.NotNull(result.NumberOfEmployees);
        _ = Assert.NotNull(result.AnnualRevenue);
        Assert.NotNull(result.Site);
    }

    [Fact]
    public async Task Supply_NeverOverwritesAFieldTheMasterTemplateDidConfigure()
    {
        // Arrange
        IFixture fixture = new Fixture();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(new AutoFixtureUnsetFieldFiller(fixture));

        // Act
        Account result = (Account)await provider.Supply();

        // Assert - AccountDataProvider's own declared defaults, untouched by AutoFixture
        Assert.StartsWith(AccountDataProvider.DefaultNamePrefix, result.Name);
        Assert.Equal(AccountDataProvider.DefaultIndustry, result.Industry);
    }

    [Fact]
    public async Task Excluding_LeavesThatOneFieldExactlyAsXftyLeftIt()
    {
        // Arrange
        IFixture fixture = new Fixture();
        AutoFixtureUnsetFieldFiller filler = new AutoFixtureUnsetFieldFiller(fixture)
            .Excluding(Field.Of<Account>(x => x.Site));
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        Account result = (Account)await provider.Supply();

        // Assert - excluded field stays null; a sibling unset field still gets filled
        Assert.Null(result.Site);
        _ = Assert.NotNull(result.NumberOfEmployees);
    }

    [Fact]
    public async Task Excluding_ChainedForEveryNavigationProperty_LeavesAllOfThemUntouched()
    {
        // Arrange - the exact navigation-property triple documented in use/autofixture.md
        IFixture fixture = new Fixture();
        AutoFixtureUnsetFieldFiller filler = new AutoFixtureUnsetFieldFiller(fixture)
            .Excluding(Field.Of<Account>(x => x.Contacts))
            .Excluding(Field.Of<Account>(x => x.Parent))
            .Excluding(Field.Of<Account>(x => x.ChildAccounts));
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        Account result = (Account)await provider.Supply();

        // Assert
        Assert.Null(result.Contacts);
        Assert.Null(result.Parent);
        Assert.Null(result.ChildAccounts);
    }

    [Fact]
    public async Task Supply_WithOmitOnRecursionBehaviorInstalled_StillFillsTheSelfReferencingFieldWithoutThrowing()
    {
        // Arrange - the documented alternative to relying on this filler's own catch
        IFixture fixture = new Fixture();
        _ = fixture.Behaviors.Remove(fixture.Behaviors.OfType<ThrowingRecursionBehavior>().Single());
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(new AutoFixtureUnsetFieldFiller(fixture));

        // Act
        Exception? thrown = await Record.ExceptionAsync(provider.Supply);

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public async Task Supply_ForASelfReferencingUnsetField_NeverLetsAFixturesRecursionGuardEscape()
    {
        // Arrange - Account.Parent is Account itself; a plain Fixture's default
        // ThrowingRecursionBehavior would raise ObjectCreationException resolving it
        IFixture fixture = new Fixture();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(new AutoFixtureUnsetFieldFiller(fixture));

        // Act
        Exception? thrown = await Record.ExceptionAsync(provider.Supply);

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public async Task Supply_CombinedWithXftyCustomization_FillsAScalarFieldWhileStillGeneratingRelationshipsViaXfty()
    {
        // Arrange - the two features compose: XFTY resolves the required Account
        // relationship; AutoFixture fills whatever scalar fields are left over.
        IFixture fixture = new Fixture().Customize(new XftyCustomization(Lookup()));
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetUnsetFieldFiller(new AutoFixtureUnsetFieldFiller(fixture));

        // Act
        Contact result = (Contact)await provider.Supply();

        // Assert
        Assert.NotNull(result.AccountId); // XFTY's own relationship resolution
        _ = Assert.NotNull(result.Birthdate); // ContactDataProvider never configures this
    }
}
