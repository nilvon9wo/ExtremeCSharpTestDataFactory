using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Test.Lookup;

/// <summary>Proves DiscriminatorLookupKey - the record-type-discriminator analog built over FlavouredLookupKey.</summary>
public class DiscriminatorLookupKeyTest
{
    [Fact]
    public void Get_ForARecordMatchingTheDiscriminatorValue_IsInstanceOfIsTrue()
    {
        // Arrange
        ILookupKey key = DiscriminatorLookupKey.Get<Account>(x => x.Type, "Person");

        // Act
        bool isInstance = key.IsInstanceOf(new Account { Type = "Person" });

        // Assert
        Assert.True(isInstance);
    }

    [Fact]
    public void Get_ForARecordWithADifferentDiscriminatorValue_IsInstanceOfIsFalse()
    {
        // Arrange
        ILookupKey key = DiscriminatorLookupKey.Get<Account>(x => x.Type, "Person");

        // Act
        bool isInstance = key.IsInstanceOf(new Account { Type = "Business" });

        // Assert
        Assert.False(isInstance);
    }

    [Fact]
    public void Get_CalledTwiceForTheSameFieldAndValue_ReturnsTheSameFlyweightAndStaysCorrect()
    {
        // Arrange / Act - calling it twice must not double-register the predicate
        FlavouredLookupKey first = DiscriminatorLookupKey.Get<Account>(x => x.Industry, "Technology");
        FlavouredLookupKey second = DiscriminatorLookupKey.Get<Account>(x => x.Industry, "Technology");

        // Assert
        Assert.Same(first, second);
        Assert.True(second.IsInstanceOf(new Account { Industry = "Technology" }));
    }

    [Fact]
    public void Get_ResolvesTheRightProviderThroughAProviderLookup()
    {
        // Arrange
        ILookupKey personKey = DiscriminatorLookupKey.Get<Account>(x => x.Type, "PersonAcct");
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [personKey] = new PersonAccountProvider(),
        });
        RecordProvider provider = new(new Account { Type = "PersonAcct" }, lookup);

        // Act
        Account result = (Account)provider.Supply();

        // Assert
        Assert.Equal("Person Default", result.Name);
    }
}

file sealed class PersonAccountProvider()
    : SimpleRecordProvider<Account>(
        new MasterTemplate<Account>(x => x.Id)
        {
            [x => x.Name] = "Person Default",
        });
