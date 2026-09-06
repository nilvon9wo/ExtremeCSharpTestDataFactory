using global::AutoFixture;
using Net.Nowhereatall.Xfty.AutoFixture;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.AutoFixture.Test;

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
}
