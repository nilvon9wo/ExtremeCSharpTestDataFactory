using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;

namespace Net.Nowhereatall.Xfty.Test.Demo;

/// <summary>
/// Proves AccountDataProvider - its identity, its Master Template defaults,
/// and a generated Account carrying them. This port has no persistence layer,
/// so Apex's NOW/DML-backed test is adapted to Mock, which proves the same
/// wiring without a database.
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
    public void CreateBundle_ProducesAnAccountWithTheDocumentedDefaults()
    {
        // Arrange
        AccountDataProvider provider = new();
        GenerationContext context = new(Lookup, InsertMode.Mock, InsertInclusivity.None);

        // Act
        Bundle bundle = provider.CreateBundle(context, [new Account()]);

        // Assert
        Account generatedAccount = (Account)bundle.GetList<Account>(x => x.Id)![0];
        Assert.NotNull(generatedAccount.Id);
        Assert.Equal(AccountDataProvider.DefaultIndustry, generatedAccount.Industry);
        Assert.Equal(AccountDataProvider.DefaultShippingCountry, generatedAccount.ShippingCountry);
    }

    [Fact]
    public void Supply_InMockMode_GeneratesARecordWithDefaults()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup).SetInsertMode(InsertMode.Mock);

        // Act
        Account generatedAccount = (Account)provider.Supply();

        // Assert
        Assert.NotNull(generatedAccount.Id);
        Assert.StartsWith(AccountDataProvider.DefaultNamePrefix, generatedAccount.Name);
        Assert.Equal(AccountDataProvider.DefaultIndustry, generatedAccount.Industry);
    }
}
