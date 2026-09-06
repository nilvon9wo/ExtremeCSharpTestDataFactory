using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Test.Demo;

/// <summary>Proves the starter-kit lookup resolves its two bundled Providers and derives keys from a record's type.</summary>
public class DefaultProviderLookupTest
{
    [Fact]
    public void Get_ForAccount_ReturnsTheAccountDataProvider()
    {
        // Arrange
        DefaultProviderLookup lookup = new();

        // Act
        IRecordProvider provider = lookup.Get(typeof(Account));

        // Assert
        _ = Assert.IsType<AccountDataProvider>(provider);
    }

    [Fact]
    public void Get_ForContact_ReturnsTheContactDataProvider()
    {
        // Arrange
        DefaultProviderLookup lookup = new();

        // Act
        IRecordProvider provider = lookup.Get(typeof(Contact));

        // Assert
        _ = Assert.IsType<ContactDataProvider>(provider);
    }

    [Fact]
    public void Get_IsCached_ReturnsTheSameInstanceOnASecondCall()
    {
        // Arrange
        DefaultProviderLookup lookup = new();

        // Act
        IRecordProvider first = lookup.Get(typeof(Account));
        IRecordProvider second = lookup.Get(typeof(Account));

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void KeysFor_ForAContactRecord_ReturnsOnlyTheContactKey()
    {
        // Arrange
        DefaultProviderLookup lookup = new();
        Contact record = new();

        // Act
        ISet<ILookupKey> keys = lookup.KeysFor(record);

        // Assert
        ILookupKey key = Assert.Single(keys);
        Assert.Equal(typeof(Contact), key.RecordType);
    }

    [Fact]
    public void Get_ForAnUnregisteredType_Throws()
    {
        // Arrange
        DefaultProviderLookup lookup = new();

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => lookup.Get(typeof(string)));

        // Assert
        Assert.Contains("No data provider registered", thrown.Message);
    }
}
