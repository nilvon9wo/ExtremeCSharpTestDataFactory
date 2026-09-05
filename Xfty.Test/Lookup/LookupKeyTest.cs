using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Test.Lookup;

/// <summary>
/// Proves the lookup-key types (LookupKey, FlavouredLookupKey) and
/// ProviderLookups resolution. The predicate building blocks have their own
/// coverage in Predicates/.
///
/// Not ported: Apex's XFTY_RecordTypeLookupKey section, and the three
/// FlavouredLookupKey tests that pass a record-type discriminator - RecordType
/// matching has no C# analog (documented capability gap; see csharp-port-idea.md).
/// This port's FlavouredLookupKey never carried a record-type discriminator at
/// all, so every other flavoured-key test here is already the "no record
/// type" case Apex tests separately.
/// </summary>
public class LookupKeyTest
{
    // Flavoured keys are interned flyweights whose .Matching(...) predicates
    // mutate the shared instance - build each exactly once, here.
    private static readonly FlavouredLookupKey EnterpriseFlavour =
        FlavouredLookupKey.Get(typeof(Account), "enterprise").Matching(FieldPredicateFactory.GreaterThan(Field.Of<Account>(nameof(Account.NumberOfEmployees)), 500));

    private static readonly FlavouredLookupKey NamedFlavour =
        FlavouredLookupKey.Get(typeof(Account), "named-runner").Matching(FieldPredicateFactory.IsNotNull(Field.Of<Account>(nameof(Account.Name))));

    private static readonly FlavouredLookupKey BigAccount =
        FlavouredLookupKey.Get(typeof(Account), "big").Matching(FieldPredicateFactory.GreaterThan(Field.Of<Account>(nameof(Account.NumberOfEmployees)), 100));

    // LookupKey ---------------------------------------------------------------

    [Fact]
    public void HashKey_ForAPlainKey_IsTheRecordTypeName()
    {
        // Arrange
        LookupKey key = LookupKey.Get(typeof(Account));

        // Act
        string hashKey = key.HashKey;

        // Assert
        Assert.Contains("Account", hashKey);
    }

    [Fact]
    public void SObjectType_ForAPlainKey_IsTheTypeItWasBuiltFor()
    {
        // Arrange
        LookupKey key = LookupKey.Get(typeof(Account));

        // Act
        Type type = key.SObjectType;

        // Assert
        Assert.Equal(typeof(Account), type);
    }

    [Fact]
    public void Specificity_ForAPlainKey_IsZero()
    {
        // Arrange
        LookupKey key = LookupKey.Get(typeof(Account));

        // Act
        int specificity = key.Specificity;

        // Assert
        Assert.Equal(0, specificity);
    }

    [Fact]
    public void IsInstanceOf_WhenTheRecordIsOfThatType_ReturnsTrue() => AssertPlainKeyIsInstanceOf(new Account(), true);

    [Fact]
    public void IsInstanceOf_WhenTheRecordIsADifferentType_ReturnsFalse() => AssertPlainKeyIsInstanceOf(new Contact(), false);

    [Fact]
    public void IsInstanceOf_WhenTheRecordIsNull_ReturnsFalse() => AssertPlainKeyIsInstanceOf(null, false);

    [Fact]
    public void Get_ForATypeAndForARecordOfThatType_ReturnsTheOneInternedInstance()
    {
        // Arrange - nothing to arrange, the flyweight is under test

        // Act
        LookupKey fromType = LookupKey.Get(typeof(Account));

        // Assert - Get(type) and Get(record) intern the same key
        Assert.Equal(fromType, LookupKey.Get(new Account()));
    }

    [Fact]
    public void Get_WhenTheTypeIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => LookupKey.Get((Type?)null));

        // Assert - a null type must be rejected
        Assert.Contains("requires a record type", thrown.Message);
    }

    // FlavouredLookupKey --------------------------------------------------

    [Fact]
    public void IsInstanceOf_WhenEveryPredicateHolds_ReturnsTrue() => AssertEnterpriseFlavourIsInstanceOf(new Account { NumberOfEmployees = 1000 }, true);

    [Fact]
    public void IsInstanceOf_WhenAPredicateFails_ReturnsFalse() => AssertEnterpriseFlavourIsInstanceOf(new Account { NumberOfEmployees = 10 }, false);

    [Fact]
    public void IsInstanceOf_WhenThePredicatedFieldIsBlank_ReturnsFalse() => AssertEnterpriseFlavourIsInstanceOf(new Account(), false);

    [Fact]
    public void IsInstanceOf_ForAFlavouredKey_WhenTheRecordIsADifferentType_ReturnsFalse() => AssertEnterpriseFlavourIsInstanceOf(new Contact(), false);

    [Fact]
    public void IsInstanceOf_WhenAPredicateHolds_ReturnsTrue() => AssertNamedFlavourIsInstanceOf(new Account { Name = "x" }, true);

    [Fact]
    public void IsInstanceOf_WhenThePredicateFails_ReturnsFalse() => AssertNamedFlavourIsInstanceOf(new Account(), false);

    [Fact]
    public void HashKey_ForAFlavouredKey_IsTypeAndFlavour()
    {
        // Arrange
        FlavouredLookupKey key = FlavouredLookupKey.Get(typeof(Account), "named");

        // Act
        string hashKey = key.HashKey;

        // Assert
        Assert.Contains("Account", hashKey);
        Assert.Contains("named", hashKey);
    }

    [Fact]
    public void Specificity_ForAFlavouredKey_GrowsWithEachPredicateAndBeatsAPlainKey()
    {
        // Arrange
        FlavouredLookupKey onePredicate = FlavouredLookupKey.Get(typeof(Account), "hashkey-a").Matching(FieldPredicateFactory.IsNotNull(Field.Of<Account>(nameof(Account.Name))));
        FlavouredLookupKey twoPredicates = FlavouredLookupKey.Get(typeof(Account), "hashkey-b")
            .Matching(FieldPredicateFactory.IsNotNull(Field.Of<Account>(nameof(Account.Name))))
            .Matching(FieldPredicateFactory.IsNotNull(Field.Of<Account>(nameof(Account.Industry))));

        // Act
        int oneSpecificity = onePredicate.Specificity;

        // Assert
        Assert.True(twoPredicates.Specificity > oneSpecificity); // more predicates = more specific
        Assert.True(oneSpecificity > LookupKey.Get(typeof(Account)).Specificity); // more specific than a plain key
    }

    [Fact]
    public void IsInstanceOf_WhenTheFlavourHasNoPredicates_ReturnsFalse()
    {
        // Arrange
        FlavouredLookupKey key = FlavouredLookupKey.Get(typeof(Account), "no-discriminator");

        // Act
        bool matches = key.IsInstanceOf(new Account());

        // Assert - a flavour with nothing on the record to match can only be used explicitly
        Assert.False(matches);
    }

    // Lookup keys as dictionary keys -------------------------------------------

    [Fact]
    public void Get_OnADictionaryKeyedByLookupKey_FindsTheEntryByValueEquality()
    {
        // Arrange
        Dictionary<ILookupKey, string> byKey = new()
        {
            [LookupKey.Get(typeof(Account))] = "plain",
            [FlavouredLookupKey.Get(typeof(Account), "hashkey-map")] = "flavoured",
        };

        // Act
        string plain = byKey[LookupKey.Get(typeof(Account))];

        // Assert
        Assert.Equal("plain", plain);
        // a freshly-built flavoured key with the same hash still matches
        Assert.Equal("flavoured", byKey[FlavouredLookupKey.Get(typeof(Account), "hashkey-map")]);
    }

    // ProviderLookups -------------------------------------------------------

    [Fact]
    public void Get_ForARegisteredKey_ReturnsTheProviderAndCachesTheInstance()
    {
        // Arrange
        IProviderLookup lookup = ProviderLookups.OfTypes(new Dictionary<ILookupKey, Type> { [LookupKey.Get(typeof(Account))] = typeof(AccountDataProvider) });

        // Act
        IRecordProvider first = lookup.Get(LookupKey.Get(typeof(Account)));

        // Assert
        _ = Assert.IsType<AccountDataProvider>(first);
        Assert.Same(first, lookup.Get(typeof(Account))); // Get(Type) resolves the same way and caches
    }

    [Fact]
    public void Get_OnAnInstanceMapLookup_ReturnsTheRegisteredProvider()
    {
        // Arrange
        IRecordProvider provider = new AccountDataProvider();
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get(typeof(Account))] = provider });

        // Act
        IRecordProvider resolved = lookup.Get(typeof(Account));

        // Assert
        Assert.Same(provider, resolved);
    }

    [Fact]
    public void Get_ForAnUnregisteredKeyOnATypeMapLookup_Throws()
    {
        // Arrange
        IProviderLookup lookup = ProviderLookups.OfTypes([]);

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => lookup.Get(FlavouredLookupKey.Get(typeof(Account), "unregistered")));

        // Assert
        Assert.Contains("Account", thrown.Message);
    }

    [Fact]
    public void Get_ForAnUnregisteredKeyOnAnInstanceMapLookup_Throws()
    {
        // Arrange
        IProviderLookup lookup = ProviderLookups.Of([]);

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => lookup.Get(typeof(Contact)));

        // Assert
        Assert.Contains("Contact", thrown.Message);
    }

    [Fact]
    public void Get_WhenTheKeyIsNull_Throws()
    {
        // Arrange
        Dictionary<ILookupKey, IRecordProvider> instances = [];

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => ProviderLookups.Get(instances, null!));

        // Assert
        Assert.Contains("lookup key is required", thrown.Message);
    }

    [Fact]
    public void Get_OnACachingTypeMapLookupWhenTheKeyIsNull_Throws()
    {
        // Arrange
        Dictionary<ILookupKey, Type> types = [];
        Dictionary<ILookupKey, IRecordProvider> cache = [];

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => ProviderLookups.Get(types, cache, null!));

        // Assert
        Assert.Contains("lookup key is required", thrown.Message);
    }

    [Fact]
    public void KeysFor_WhenGivenANullRecord_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => ProviderLookups.KeysFor(new HashSet<ILookupKey>(), null));

        // Assert
        Assert.Contains("record is required", thrown.Message);
    }

    [Fact]
    public void KeysFor_SkipsKeysRegisteredForOtherRecordTypes()
    {
        // Arrange
        HashSet<ILookupKey> registered = [LookupKey.Get(typeof(Account)), LookupKey.Get(typeof(Contact))];

        // Act
        ISet<ILookupKey> matches = ProviderLookups.KeysFor(registered, new Account());

        // Assert
        ILookupKey match = Assert.Single(matches);
        Assert.Equal(typeof(Account), match.SObjectType);
    }

    [Fact]
    public void KeysFor_WhenARecordMatchesARefinedKey_ReturnsBothItAndThePlainKey() =>
        AssertKeysForHashes(new Account { NumberOfEmployees = 500 }, 2);

    [Fact]
    public void KeysFor_WhenARecordMatchesOnlyThePlainKey_ReturnsJustThePlainKey() =>
        AssertKeysForHashes(new Account { NumberOfEmployees = 1 }, 1);

    [Fact]
    public void Resolve_WhenARefinedKeyMatches_PicksTheMostSpecific()
    {
        // Arrange
        IProviderLookup lookup = ProviderLookups.OfTypes(new Dictionary<ILookupKey, Type>
        {
            [LookupKey.Get(typeof(Account))] = typeof(AccountDataProvider),
            [BigAccount] = typeof(AccountDataProvider),
        });

        // Act
        ILookupKey resolved = ProviderLookups.Resolve(lookup, new Account { NumberOfEmployees = 500 });

        // Assert
        Assert.Equal(BigAccount.HashKey, resolved.HashKey);
    }

    [Fact]
    public void Resolve_WhenNothingRefinedMatches_PicksThePlainKey()
    {
        // Arrange
        IProviderLookup lookup = ProviderLookups.OfTypes(new Dictionary<ILookupKey, Type>
        {
            [LookupKey.Get(typeof(Account))] = typeof(AccountDataProvider),
            [BigAccount] = typeof(AccountDataProvider),
        });

        // Act
        ILookupKey resolved = ProviderLookups.Resolve(lookup, new Account { NumberOfEmployees = 1 });

        // Assert
        Assert.Equal(LookupKey.Get(typeof(Account)).HashKey, resolved.HashKey);
    }

    [Fact]
    public void Resolve_WhenTwoEquallySpecificKeysMatch_Throws()
    {
        // Arrange
        IProviderLookup lookup = ProviderLookups.OfTypes(new Dictionary<ILookupKey, Type>
        {
            [FlavouredLookupKey.Get(typeof(Account), "ambiguous-a").Matching(FieldPredicateFactory.IsNotNull(Field.Of<Account>(nameof(Account.Name))))] = typeof(AccountDataProvider),
            [FlavouredLookupKey.Get(typeof(Account), "ambiguous-b").Matching(FieldPredicateFactory.IsNotNull(Field.Of<Account>(nameof(Account.Name))))] = typeof(AccountDataProvider),
        });

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => ProviderLookups.Resolve(lookup, new Account { Name = "x" }));

        // Assert
        Assert.Contains("Ambiguous", thrown.Message);
    }

    // Runners + helpers ------------------------------------------------

    private static void AssertPlainKeyIsInstanceOf(object? record, bool expected)
    {
        // Arrange
        LookupKey key = LookupKey.Get(typeof(Account));

        // Act
        bool matches = key.IsInstanceOf(record);

        // Assert
        Assert.Equal(expected, matches);
    }

    private static void AssertEnterpriseFlavourIsInstanceOf(object? record, bool expected)
    {
        // Act
        bool matches = EnterpriseFlavour.IsInstanceOf(record);

        // Assert
        Assert.Equal(expected, matches);
    }

    private static void AssertNamedFlavourIsInstanceOf(object? record, bool expected)
    {
        // Act
        bool matches = NamedFlavour.IsInstanceOf(record);

        // Assert
        Assert.Equal(expected, matches);
    }

    private static void AssertKeysForHashes(Account record, int expectedCount)
    {
        // Arrange
        IProviderLookup lookup = ProviderLookups.OfTypes(new Dictionary<ILookupKey, Type>
        {
            [LookupKey.Get(typeof(Account))] = typeof(AccountDataProvider),
            [BigAccount] = typeof(AccountDataProvider),
        });

        // Act
        ISet<ILookupKey> matches = lookup.KeysFor(record);

        // Assert
        Assert.Equal(expectedCount, matches.Count);
    }
}
