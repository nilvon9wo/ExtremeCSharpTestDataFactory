using global::AutoFixture;
using Net.NowhereAtAll.Xfty.AutoFixture;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.AutoFixture.Test;

/// <summary>
/// Proves XftyCustomization/XftySpecimenBuilder - pointing fixture.Create&lt;T&gt;()
/// at a registered RecordProvider instead of AutoFixture's own generation.
/// </summary>
public class XftyCustomizationTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    private static IFixture Fixture() => new Fixture().Customize(new XftyCustomization(Lookup()));

    [Fact]
    public void Create_ForATypeWithARegisteredProvider_ReturnsAnXftyGeneratedRecord()
    {
        // Arrange
        IFixture fixture = Fixture();

        // Act
        Account account = fixture.Create<Account>();

        // Assert - AccountDataProvider's own Master Template default, not an AutoFixture-generated string
        Assert.StartsWith(AccountDataProvider.DefaultNamePrefix, account.Name);
    }

    [Fact]
    public void Create_ForARecordWithARequiredRelationship_ResolvesItViaXftyToo()
    {
        // Arrange
        IFixture fixture = Fixture();

        // Act
        Contact contact = fixture.Create<Contact>();

        // Assert - RecordProvider.Supply()'s own default inclusivity generates required relationships
        Assert.NotNull(contact.AccountId);
    }

    [Fact]
    public void Create_ForATypeWithNoRegisteredProvider_FallsThroughToAutoFixturesOwnGeneration()
    {
        // Arrange
        IFixture fixture = Fixture();

        // Act
        string generated = fixture.Create<string>();

        // Assert - AutoFixture's own default: a non-empty, non-null string
        Assert.False(string.IsNullOrEmpty(generated));
    }

    [Fact]
    public void CreateMany_ForATypeWithARegisteredProvider_GeneratesADistinctRecordEachTime()
    {
        // Arrange
        IFixture fixture = Fixture();

        // Act
        List<Account> accounts = [.. fixture.CreateMany<Account>(3)];

        // Assert - each Supply() call mints its own mocked Id
        Assert.Equal(3, accounts.Select(account => account.Id).Distinct().Count());
    }

    [Fact]
    public void Create_DefaultsToMockInsertMode_SoEveryRecordHasAnId()
    {
        // Arrange
        IFixture fixture = Fixture();

        // Act
        Account account = fixture.Create<Account>();

        // Assert
        Assert.NotNull(account.Id);
    }

    [Fact]
    public void Create_WithInclusivityOverriddenToNone_LeavesTheRelationshipUngenerated()
    {
        // Arrange - overriding back to RecordProvider's own defaults, explicitly
        IFixture fixture = new Fixture().Customize(new XftyCustomization(Lookup(), InsertMode.Mock, InsertInclusivity.None));

        // Act
        Contact contact = fixture.Create<Contact>();

        // Assert
        Assert.Null(contact.AccountId);
    }
}
