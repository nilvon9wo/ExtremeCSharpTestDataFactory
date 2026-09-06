using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Test.Relationships;

/// <summary>Proves DefaultRelationship - its accessors and its deferred, memoised lookup-key resolution. Pure in-memory, no persistence.</summary>
public class DefaultRelationshipTest
{
    [Fact]
    public void OverrideTemplate_ReturnsTheTemplateItWasBuiltWith()
    {
        // Arrange
        Account template = new() { Name = "Parent" };

        // Act
        DefaultRelationship relationship = new(template);

        // Assert
        Assert.Same(template, relationship.OverrideTemplate);
        Assert.Null(relationship.RelatedField); // no related field was supplied
    }

    [Fact]
    public void RelatedField_WhenBuiltWithARelatedField_ReturnsIt()
    {
        // Arrange
        // nothing to arrange

        // Act
        DefaultRelationship relationship = new(new Account { Name = "Parent" }, Field.Of<Account>(x => x.AccountNumber));

        // Assert
        Assert.Equal(Field.Of<Account>(x => x.AccountNumber), relationship.RelatedField);
    }

    [Fact]
    public void ResolveLookupKey_WhenAnExplicitKeyWasGiven_ReturnsItAsIs()
    {
        // Arrange
        ILookupKey explicitKey = FlavouredLookupKey.Get(typeof(Account), "big");
        DefaultRelationship relationship = new(explicitKey, new Account());

        // Act
        ILookupKey? resolved = relationship.ResolveLookupKey(new CountingLookup());

        // Assert
        Assert.Equal(explicitKey, resolved);
    }

    [Fact]
    public void ResolveLookupKey_WhenNoKeyWasGiven_DerivesItFromTheTemplateOnceAndMemoises()
    {
        // Arrange
        DefaultRelationship relationship = new(new Account());
        CountingLookup lookup = new();

        // Act
        ILookupKey? firstCall = relationship.ResolveLookupKey(lookup);
        ILookupKey? secondCall = relationship.ResolveLookupKey(lookup);

        // Assert
        Assert.Equal(1, lookup.KeysForCalls); // derivation happens only once
        Assert.Equal(firstCall, secondCall); // the memoised key is returned on later calls
        Assert.Contains("Account", firstCall!.HashKey);
    }
}

file sealed class CountingLookup : IProviderLookup
{
    public int KeysForCalls { get; private set; }

    public IRecordProvider Get(Type recordType) => null!;

    public IRecordProvider Get(ILookupKey lookupKey) => null!;

    public ISet<ILookupKey> KeysFor(object? record)
    {
        this.KeysForCalls++;
        return new HashSet<ILookupKey>();
    }
}
