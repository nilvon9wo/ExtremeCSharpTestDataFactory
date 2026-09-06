using global::AutoBogus;
using Net.Nowhereatall.Xfty.AutoBogus;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.AutoBogus.Test;

/// <summary>
/// Proves XftyAutoBogus/XftyAutoBogusOverride - pointing AutoBogus's own
/// generation at a registered RecordProvider instead of its own.
/// </summary>
public class XftyAutoBogusTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void Generate_ForATypeWithARegisteredProvider_ReturnsAnXftyGeneratedRecord()
    {
        // Arrange
        IAutoFaker faker = XftyAutoBogus.CreateFaker(Lookup());

        // Act
        Account account = faker.Generate<Account>();

        // Assert - AccountDataProvider's own Master Template default, not an AutoBogus-generated string
        Assert.StartsWith(AccountDataProvider.DefaultNamePrefix, account.Name);
    }

    [Fact]
    public void Generate_ForARecordWithARequiredRelationship_ResolvesItViaXftyToo()
    {
        // Arrange
        IAutoFaker faker = XftyAutoBogus.CreateFaker(Lookup());

        // Act
        Contact contact = faker.Generate<Contact>();

        // Assert - CreateFaker's own default inclusivity generates required relationships
        Assert.NotNull(contact.AccountId);
    }

    [Fact]
    public void Generate_ForATypeWithNoRegisteredProvider_FallsThroughToAutoBogussOwnGeneration()
    {
        // Arrange
        IAutoFaker faker = XftyAutoBogus.CreateFaker(Lookup());

        // Act
        string generated = faker.Generate<string>();

        // Assert - AutoBogus's own default: a non-empty, non-null string
        Assert.False(string.IsNullOrEmpty(generated));
    }

    [Fact]
    public void Generate_Many_ForATypeWithARegisteredProvider_GeneratesADistinctRecordEachTime()
    {
        // Arrange
        IAutoFaker faker = XftyAutoBogus.CreateFaker(Lookup());

        // Act
        List<Account> accounts = faker.Generate<Account>(3);

        // Assert - each Supply() call mints its own mocked Id
        Assert.Equal(3, accounts.Select(account => account.Id).Distinct().Count());
    }

    [Fact]
    public void Generate_DefaultsToMockInsertMode_SoEveryRecordHasAnId()
    {
        // Arrange
        IAutoFaker faker = XftyAutoBogus.CreateFaker(Lookup());

        // Act
        Account account = faker.Generate<Account>();

        // Assert
        Assert.NotNull(account.Id);
    }

    [Fact]
    public void Generate_WithInclusivityOverriddenToNone_LeavesTheRelationshipUngenerated()
    {
        // Arrange - overriding back to RecordProvider's own defaults, explicitly
        IAutoFaker faker = XftyAutoBogus.CreateFaker(Lookup(), InsertMode.Mock, InsertInclusivity.None);

        // Act
        Contact contact = faker.Generate<Contact>();

        // Assert
        Assert.Null(contact.AccountId);
    }
}
