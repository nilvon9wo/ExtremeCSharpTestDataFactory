using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>Proves <see cref="RecordProvider{TRecord}"/> - the typed wrapper - chains and returns TRecord with no cast.</summary>
public class RecordProviderOfTTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public void Supply_ReturnsTheTypedRecord_WithNoCast()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .Put(x => x.FirstName, "Alice")
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = provider.Supply();

        // Assert
        Assert.Equal("Alice", result.FirstName);
    }

    [Fact]
    public void SupplyList_ReturnsATypedList()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .SetQuantityPerTemplate(3)
            .SetInsertMode(InsertMode.Mock);

        // Act
        List<Contact> result = provider.SupplyList();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void SupplyBundle_StillReturnsAPlainBundle()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Assert.NotNull(bundle.GetList<Contact>(x => x.Id));
    }

    [Fact]
    public void ObjectInitializer_RoutesEachValueByRuntimeType()
    {
        // Arrange - mirrors MasterTemplate<TRecord>'s own indexer syntax
        RecordProvider<Contact> provider = new(Lookup)
        {
            [x => x.FirstName] = "Alice",
            [x => x.Department] = CopyFromSiblingExpression.From<Contact>(x => x.FirstName),
        };

        // Act
        Contact result = provider
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert
        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Alice", result.Department);
    }

    [Fact]
    public void ObjectInitializer_WhenGivenARelationship_Throws()
    {
        // Arrange - relationships must state their own requiredness; the indexer can't infer it
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => new RecordProvider<Contact>(Lookup)
        {
            [x => x.AccountId] = new DefaultRelationship(new Account()),
        });

        // Assert
        Assert.Contains("PutRequired", thrown.Message);
    }

    [Fact]
    public void ConvertsImplicitlyToThePlainRecordProvider()
    {
        // Arrange
        RecordProvider<Contact> typed = new(Lookup);

        // Act
        RecordProvider plain = typed;

        // Assert
        Assert.NotNull(plain);
    }
}
