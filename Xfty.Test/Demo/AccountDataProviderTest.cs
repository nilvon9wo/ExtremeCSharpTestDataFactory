using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.Test.Demo;

/// <summary>
/// Proves AccountDataProvider - its identity, its Master Template defaults,
/// and a generated Account carrying them. Uses Mock rather than Now/a real
/// persistence gateway, since the wiring under test is unaffected by whether
/// anything actually gets saved - see PersistenceGatewayTest for the
/// insert-mode proof itself.
/// </summary>
public class AccountDataProviderTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public void PrimaryTargetField_IsAccountId()
    {
        // Arrange
        AccountDataProvider provider = new();

        // Act
        System.Reflection.PropertyInfo primaryField = provider.PrimaryTargetField;

        // Assert
        Assert.Equal(Field.Of<Account>(x => x.Id), primaryField);
        Assert.NotNull(provider.MasterTemplate);
    }

    [Fact]
    public async Task CreateBundle_ProducesAnAccountWithTheDocumentedDefaults()
    {
        // Arrange
        AccountDataProvider provider = new();
        GenerationContext context = new(Lookup, InsertMode.Mock, InsertInclusivity.None);

        // Act
        Bundle bundle = await provider.CreateBundle(context, [new Account()]);

        // Assert
        Account generatedAccount = (Account)bundle.GetList<Account>(x => x.Id)![0];
        Assert.NotNull(generatedAccount.Id);
        Assert.Equal(AccountDataProvider.DefaultIndustry, generatedAccount.Industry);
        Assert.Equal(AccountDataProvider.DefaultShippingCountry, generatedAccount.ShippingCountry);
    }

    [Fact]
    public async Task Supply_InMockMode_GeneratesARecordWithDefaults()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup).SetInsertMode(InsertMode.Mock);

        // Act
        Account generatedAccount = (Account)await provider.Supply();

        // Assert
        Assert.NotNull(generatedAccount.Id);
        Assert.StartsWith(AccountDataProvider.DefaultNamePrefix, generatedAccount.Name);
        Assert.Equal(AccountDataProvider.DefaultIndustry, generatedAccount.Industry);
    }
}
