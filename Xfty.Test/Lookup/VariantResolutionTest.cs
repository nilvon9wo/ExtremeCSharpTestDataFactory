using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Predicates;

namespace Net.NowhereAtAll.Xfty.Test.Lookup;

/// <summary>
/// Proves how the two ways to name a Provider variant - an explicit lookup
/// key and an override template - are reconciled: ProviderLookups.Reconcile
/// directly, and RecordProvider end to end. Mock mode throughout.
/// </summary>
public class VariantResolutionTest
{
    private static readonly ILookupKey Big =
        FlavouredLookupKey.Get(typeof(Account), "reconcile-big").Matching(FieldPredicateFactory.GreaterThan<Account>(x => x.NumberOfEmployees, 1000));

    private static readonly ILookupKey Small =
        FlavouredLookupKey.Get(typeof(Account), "reconcile-small").Matching(FieldPredicateFactory.LessThan<Account>(x => x.NumberOfEmployees, 10));

    private static IProviderLookup Lookup() =>
        ProviderLookups.OfTypes(new Dictionary<ILookupKey, Type>
        {
            [LookupKey.Get(typeof(Account))] = typeof(AccountDataProvider),
            [Big] = typeof(AccountDataProvider),
            [Small] = typeof(AccountDataProvider),
        });

    // ProviderLookups.Reconcile -------------------------------------------

    [Fact]
    public void Reconcile_WhenGivenNeitherKeyNorTemplate_ReturnsNull() => AssertReconcile(null, null, null);

    [Fact]
    public void Reconcile_WhenGivenOnlyATemplate_DerivesTheKeyFromIt() =>
        AssertReconcile(null, new Account { NumberOfEmployees = 5000 }, Big.HashKey);

    [Fact]
    public void Reconcile_WhenTheTemplateAgreesWithTheExplicitKey_KeepsTheExplicitKey() =>
        AssertReconcile(Big, new Account { NumberOfEmployees = 5000 }, Big.HashKey);

    [Fact]
    public void Reconcile_WhenTheTemplateCarriesNoDiscriminator_KeepsTheExplicitKey() =>
        AssertReconcile(Big, new Account(), Big.HashKey);

    [Fact]
    public void Reconcile_WhenTheTemplateMatchesADifferentRefinedVariant_Throws()
    {
        // Arrange
        IProviderLookup providerLookup = Lookup();

        // Act
        LookupException thrown = Assert.Throws<LookupException>(() => ProviderLookups.Reconcile(providerLookup, Big, new Account { NumberOfEmployees = 2 }));

        // Assert - a template matching a different variant must be rejected
        Assert.Contains("contradicts", thrown.Message);
    }

    // End to end through RecordProvider ---------------------------

    [Fact]
    public async Task Supply_WhenWithVariantContradictsTheOverrideTemplate_Throws()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .WithVariant(Big)
            .SetOverrideTemplate(new Account { NumberOfEmployees = 2 })
            .SetInsertMode(InsertMode.Mock);

        // Act
        LookupException thrown = await Assert.ThrowsAsync<LookupException>(provider.Supply);

        // Assert - WithVariant contradicting the template must throw
        Assert.Contains("contradicts", thrown.Message);
    }

    [Fact]
    public async Task Supply_WhenWithVariantAgreesWithTheOverrideTemplate_StillAppliesTheTemplate()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .WithVariant(Big)
            .SetOverrideTemplate(new Account { NumberOfEmployees = 5000 })
            .SetInsertMode(InsertMode.Mock);

        // Act
        Account result = (Account)await provider.Supply();

        // Assert
        Assert.Equal(5000, result.NumberOfEmployees);
    }

    // Helpers -----------------------------------------------------------

    private static void AssertReconcile(ILookupKey? explicitKey, object? template, string? expectedHash)
    {
        // Arrange
        IProviderLookup providerLookup = Lookup();

        // Act
        ILookupKey? resolved = ProviderLookups.Reconcile(providerLookup, explicitKey, template);

        // Assert
        Assert.Equal(expectedHash, resolved?.HashKey);
    }
}
