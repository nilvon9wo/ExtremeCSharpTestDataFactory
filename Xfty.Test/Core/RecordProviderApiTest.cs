using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>
/// Proves the fluent public API of RecordProvider - one test per affordance:
/// the constructors and their guards, the setters, WithVariant, Put(...)
/// routing, IncludeOptional / ExcludeRelationship, precedence. Mock/Never
/// mode throughout - no persistence. End-to-end scenarios live in
/// RecordProviderScenarioTest.
/// </summary>
public class RecordProviderApiTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    private static RecordProvider ContactProvider() =>
        new RecordProvider(typeof(Contact), Lookup).SetInsertMode(InsertMode.Mock);

    // Constructor guards ------------------------------------------

    [Fact]
    public void Constructor_WhenTheRecordTypeIsNull_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => new RecordProvider((Type)null!, Lookup));

        // Assert
        Assert.Contains("record type is required", thrown.Message);
    }

    [Fact]
    public void Constructor_WhenTheProviderLookupIsNull_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => new RecordProvider(typeof(Contact), null!));

        // Assert
        Assert.Contains("Provider Lookup", thrown.Message);
    }

    [Fact]
    public void Constructor_WhenTheLookupKeyIsNull_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => new RecordProvider((ILookupKey)null!, Lookup));

        // Assert
        Assert.Contains("lookup key", thrown.Message);
    }

    [Fact]
    public void Constructor_WhenTheTemplateListIsEmpty_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => new RecordProvider([], Lookup));

        // Assert
        Assert.Contains("empty or null template list", thrown.Message);
    }

    [Fact]
    public void Constructor_WhenTheTemplateListsFirstEntryIsNull_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => new RecordProvider([null!], Lookup));

        // Assert
        Assert.Contains("empty or null template list", thrown.Message);
    }

    // Convenience constructors ----------------------------------

    [Fact]
    public async Task Constructor_FromALookupKey_PinsTheVariantAndDerivesTheType()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(LookupKey.Get(typeof(Contact)), Lookup).SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.NotNull(result.Id);
        Assert.StartsWith(ContactDataProvider.DefaultLastNamePrefix, result.LastName);
    }

    [Fact]
    public async Task Constructor_FromATemplate_DerivesTheTypeAndAppliesTheOverride()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(new Contact { FirstName = "Zoe" }, Lookup).SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.Equal("Zoe", result.FirstName);
        Assert.StartsWith(ContactDataProvider.DefaultLastNamePrefix, result.LastName);
    }

    [Fact]
    public async Task Constructor_FromATemplateList_DerivesTheTypeAndKeepsEachTemplate()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(
            [new Contact { FirstName = "Alice" }, new Contact { FirstName = "Bob" }], Lookup)
            .SetInsertMode(InsertMode.Mock);

        // Act
        List<object> results = await provider.SupplyList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Alice", ((Contact)results[0]).FirstName);
        Assert.Equal("Bob", ((Contact)results[1]).FirstName);
    }

    // Setter guards -------------------------------------------

    [Fact]
    public void SetQuantityPerTemplate_WhenGivenAValueBelowOne_Throws()
    {
        // Arrange
        RecordProvider provider = ContactProvider();

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => provider.SetQuantityPerTemplate(0));

        // Assert
        Assert.Contains("makes no sense", thrown.Message);
    }

    [Fact]
    public void SetOverrideTemplateList_WhenGivenMixedRecordTypes_Throws()
    {
        // Arrange
        RecordProvider provider = ContactProvider();

        // Act
        RecordProviderConflictException thrown = Assert.Throws<RecordProviderConflictException>(
            () => provider.SetOverrideTemplateList([new Contact(), new Account()]));

        // Assert
        Assert.Contains("Account", thrown.Message);
    }

    [Fact]
    public void SetOverrideTemplateList_WhenGivenAHomogeneousListOfTheWrongType_Throws()
    {
        // Arrange - a homogeneous list of the "wrong" type used to silently retarget the Provider
        RecordProvider provider = ContactProvider();

        // Act
        RecordProviderConflictException thrown = Assert.Throws<RecordProviderConflictException>(
            () => provider.SetOverrideTemplateList([new Account()]));

        // Assert - the constructor asked for Contact
        Assert.Contains("Contact", thrown.Message);
    }

    [Fact]
    public async Task Constructor_FromAHomogeneousTemplateList_IsAccepted()
    {
        // Arrange
        RecordProvider provider = new RecordProvider([new Contact { FirstName = "A" }, new Contact { FirstName = "B" }], Lookup)
            .SetInsertMode(InsertMode.Mock);

        // Act
        List<object> results = await provider.SupplyList();

        // Assert
        Assert.Equal(2, results.Count);
    }

    // WithVariant --------------------------------------------

    [Fact]
    public async Task WithVariant_ForAMatchingKey_PinsItAndGenerates()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .WithVariant(LookupKey.Get(typeof(Contact)))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.NotNull(result.Id);
        Assert.StartsWith(ContactDataProvider.DefaultLastNamePrefix, result.LastName);
    }

    [Fact]
    public void WithVariant_WhenTheKeyIsNull_Throws()
    {
        // Arrange
        RecordProvider provider = new(typeof(Contact), Lookup);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => provider.WithVariant(null!));

        // Assert
        Assert.Contains("variant key is required", thrown.Message);
    }

    [Fact]
    public void WithVariant_WhenTheKeyIsForAnotherRecordType_Throws()
    {
        // Arrange
        RecordProvider provider = new(typeof(Contact), Lookup);

        // Act
        RecordProviderConflictException thrown = Assert.Throws<RecordProviderConflictException>(
            () => provider.WithVariant(LookupKey.Get(typeof(Account))));

        // Assert
        Assert.Contains("Account", thrown.Message);
        Assert.Contains("Contact", thrown.Message);
    }

    [Fact]
    public void WithVariant_WhenCalledAfterPut_Throws()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup).Put<Contact>(x => x.FirstName, "x");

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => provider.WithVariant(LookupKey.Get(typeof(Contact))));

        // Assert
        Assert.Contains("WithVariant", thrown.Message);
    }

    // Put(...) routing -----------------------------------

    [Fact]
    public async Task Put_ForAValueExpressionPassedAsObject_RoutesItCorrectly()
    {
        // Arrange
        RecordProvider provider = ContactProvider().Put<Contact>(x => x.FirstName, (object)new LiteralExpression("RoutedStrategy"));

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.Equal("RoutedStrategy", result.FirstName);
    }

    [Fact]
    public async Task Put_ForAContextAwareExpressionPassedAsObject_RoutesItCorrectly()
    {
        // Arrange
        RecordProvider provider = ContactProvider()
            .Put<Contact>(x => x.FirstName, "Source")
            .Put<Contact>(x => x.Department, (object)CopyFromSiblingExpression.From<Contact>(x => x.FirstName));

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert - a context-aware expression passed as object still routes correctly
        Assert.Equal("Source", result.Department);
    }

    [Fact]
    public void Put_WhenGivenARelationship_Throws()
    {
        // Arrange
        RecordProvider provider = new(typeof(Contact), Lookup);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => provider.Put<Contact>(x => x.AccountId, (object)new DefaultRelationship(new Account())));

        // Assert
        Assert.Contains("PutRequired", thrown.Message);
    }

    [Fact]
    public async Task Put_ForAValueExpression_ReplacesTheGenerationStrategyForAField()
    {
        // Regression guard: a defect previously made provider-level Put(...) a no-op.
        // Arrange
        RecordProvider provider = ContactProvider().Put<Contact>(x => x.FirstName, new LiteralExpression("DeliberateName"));

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.Equal("DeliberateName", result.FirstName);
    }

    [Fact]
    public async Task Put_ForABareLiteral_TreatsItAsAnExactValue()
    {
        // Arrange
        RecordProvider provider = ContactProvider().Put<Contact>(x => x.FirstName, "LiteralFirstName");

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.Equal("LiteralFirstName", result.FirstName);
    }

    [Fact]
    public async Task SetOverrideTemplate_WinsOverPut()
    {
        // Arrange
        RecordProvider provider = ContactProvider()
            .Put<Contact>(x => x.FirstName, new LiteralExpression("FromPut"))
            .SetOverrideTemplate(new Contact { FirstName = "FromOverride" });

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.Equal("FromOverride", result.FirstName);
    }

    [Fact]
    public async Task Put_OnOneProvider_DoesNotLeakIntoALaterSeparateProvider()
    {
        // Arrange - customise one Provider, then build a pristine one on the same lookup
        _ = await ContactProvider().Put<Contact>(x => x.FirstName, new LiteralExpression("Customized")).Supply();

        // Act
        Contact pristine = Assert.IsType<Contact>(await ContactProvider().Supply());

        // Assert
        Assert.StartsWith(ContactDataProvider.DefaultFirstNamePrefix, pristine.FirstName);
    }

    // RemoveFromMasterTemplate -----------------------------

    [Fact]
    public async Task RemoveFromMasterTemplate_DropsAGeneratedValueAndLeavesOtherDefaults()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .RemoveFromMasterTemplate<Contact>(x => x.Email)
            .SetInsertMode(InsertMode.Never);

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.Null(result.Email); // Email is no longer generated once removed
        Assert.NotNull(result.LastName); // other defaults are unaffected
    }

    // PutOptional / IncludeOptional / ExcludeRelationship ---

    [Fact]
    public async Task PutOptional_AtRequiredInclusivity_TheOptionalRelationshipIsSkipped()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .PutOptional<Contact>(x => x.ReportsToId, new DefaultRelationship(new Contact()))
            .RemoveFromMasterTemplate<Contact>(x => x.AccountId)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - an optional relationship is skipped for Required
        Assert.Null(bundle.GetList<Contact>(x => x.ReportsToId));
    }

    [Fact]
    public async Task PutOptional_AtAllInclusivity_TheOptionalRelationshipIsGenerated()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .PutOptional<Contact>(x => x.ReportsToId, new DefaultRelationship(new Contact()))
            .RemoveFromMasterTemplate<Contact>(x => x.AccountId)
            .SetInclusivity(InsertInclusivity.All)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - an optional relationship is generated for All
        _ = Assert.Single(bundle.GetList<Contact>(x => x.ReportsToId)!);
    }

    [Fact]
    public async Task IncludeOptional_AtRequiredInclusivity_PromotesJustThatOptionalRelationship()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .PutOptional<Contact>(x => x.ReportsToId, new DefaultRelationship(new Contact()))
            .IncludeOptional<Contact>(x => x.ReportsToId)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        _ = Assert.Single(bundle.GetList<Contact>(x => x.ReportsToId)!); // the included optional relationship is generated
        _ = Assert.Single(bundle.GetList<Contact>(x => x.AccountId)!); // the required Account is still generated
    }

    [Fact]
    public async Task IncludeOptional_OnAnAlreadyRequiredRelationship_IsANoOp()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .IncludeOptional<Contact>(x => x.AccountId)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - the required Account is generated, exactly once
        _ = Assert.Single(bundle.GetList<Contact>(x => x.AccountId)!);
    }

    [Fact]
    public async Task IncludeOptional_WhenTheFieldIsNotARelationship_Throws()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .IncludeOptional<Contact>(x => x.FirstName)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        XftyConfigurationException thrown = await Assert.ThrowsAsync<XftyConfigurationException>(provider.SupplyBundle);

        // Assert
        Assert.Contains("is not a relationship", thrown.Message);
    }

    [Fact]
    public async Task ExcludeRelationship_SkipsARequiredRelationshipAndLeavesNoOrphanReference()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .ExcludeRelationship<Contact>(x => x.AccountId)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        Assert.Null(bundle.GetList<Contact>(x => x.AccountId)); // the excluded relationship is not generated
        Assert.Null(((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId); // and not left as an orphan reference
    }

    [Fact]
    public async Task ExcludeRelationship_IsInstanceLocal()
    {
        // Arrange - a separate Provider on the same lookup, no exclusion
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup)
            .ExcludeRelationship<Contact>(x => x.AccountId)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);
        Bundle normal = await new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyBundle();

        // Act
        Bundle excluded = await provider.SupplyBundle();

        // Assert
        Assert.Null(excluded.GetList<Contact>(x => x.AccountId));
        _ = Assert.Single(normal.GetList<Contact>(x => x.AccountId)!); // a separate Provider on the same lookup still generates the relationship
    }

    [Fact]
    public void ExcludeRelationship_WhenGivenAPlainValueField_Throws()
    {
        // Arrange
        RecordProvider provider = new(typeof(Contact), Lookup);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => provider.ExcludeRelationship<Contact>(x => x.FirstName));

        // Assert
        Assert.Contains("no relationship", thrown.Message);
    }

    // Supply / SupplyList ----------------------------------

    [Fact]
    public async Task Supply_ReturnsASingleRecordWithMasterTemplateDefaults()
    {
        // Arrange
        RecordProvider provider = ContactProvider();

        // Act
        Contact result = Assert.IsType<Contact>(await provider.Supply());

        // Assert
        Assert.NotNull(result.Id);
        Assert.StartsWith(ContactDataProvider.DefaultFirstNamePrefix, result.FirstName);
        Assert.StartsWith(ContactDataProvider.DefaultLastNamePrefix, result.LastName);
    }

    [Fact]
    public async Task SupplyList_AppliesQuantityOutsideTheTemplateLoop()
    {
        // Arrange
        RecordProvider provider = ContactProvider()
            .SetOverrideTemplateList([new Contact { FirstName = "Alice" }, new Contact { FirstName = "Bob" }])
            .SetQuantityPerTemplate(2);

        // Act
        List<object> results = await provider.SupplyList();

        // Assert - two templates x quantity 2
        Assert.Equal(4, results.Count);
        List<string?> firstNames = [.. results.Cast<Contact>().Select(contact => contact.FirstName)];
        Assert.Equal(["Alice", "Bob", "Alice", "Bob"], firstNames); // A, B, A, B - not A, A, B, B
    }
}
