using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.Children;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;
using NSubstitute;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves every fluent method on <see cref="RecordProvider{TRecord}"/> forwards
/// to the identically-named method on the wrapped plain <see cref="RecordProvider"/>
/// with its arguments intact, and returns the wrapper so the chain stays typed.
/// One test per forwarder (or forwarder pair), organised by the partial file it
/// lives in. The plain <see cref="RecordProvider"/>'s own semantics are proven in
/// <see cref="RecordProviderApiTest"/> / RecordProviderScenarioTest - these tests
/// only prove the delegation.
/// </summary>
public class RecordProviderOfTForwardingTest : IDisposable
{
    private static readonly DefaultProviderLookup Lookup = new();

    /// <summary>
    /// The one Deferred test registers into the process-wide DeferredInserter; clear it however that test ends.
    /// </summary>
    public void Dispose()
    {
        DeferredInserter.ResetForTesting();
        GC.SuppressFinalize(this);
    }

    // FieldConfig - PropertyInfo forms -----------------------------------

    [Fact]
    public async Task Put_ByPropertyInfo_ForwardsValueExpressionsLiteralsAndContextAwareExpressions()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .Put(Field.Of<Contact>(x => x.FirstName), new LiteralExpression("Alice"))
            .Put(Field.Of<Contact>(x => x.LastName), (object?)"Smith")
            .Put(Field.Of<Contact>(x => x.Department), CopyFromSiblingExpression.From<Contact>(x => x.FirstName))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.Equal("Alice", result.Department); // the context-aware expression copied FirstName
    }

    [Fact]
    public async Task Put_ByLambda_ForwardsADeferredExpression()
    {
        // Arrange - an up-flow value put on the wrapper; it only resolves under Deferred, and a
        // forward to the wrong Put overload would silently route it as a plain literal.
        RecordProvider<Account> provider = new RecordProvider<Account>(DepartmentChildLookup())
            .Put(x => x.Site, CopyFromDescendantExpression.From<Contact>(x => x.AccountId, x => x.Department))
            .WithChild(Field.Of<Contact>(x => x.AccountId))
            .SetInsertMode(InsertMode.Deferred);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Account account = (Account)DeferredInsertBuffer.Flatten(bundle).Records().OfType<Account>().First();
        Assert.Equal("Engineering", account.Site); // read up from the generated child Contact's Department
    }

    [Fact]
    [SuppressMessage(
        "Performance",
        "HLQ005:Avoid Single() and SingleOrDefault()",
        Justification = "one relationship generated"
    )]
    public async Task PutRequired_ByPropertyInfo_GeneratesTheRelationship()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .PutRequired(Field.Of<Contact>(x => x.ReportsToId), new DefaultRelationship(new Contact()))
            .RemoveFromMasterTemplate(Field.Of<Contact>(x => x.AccountId))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        _ = Assert.Single(bundle.GetList<Contact>(x => x.ReportsToId)!);
    }

    [Fact]
    public async Task PutOptional_ByPropertyInfo_IsSkippedAtRequiredInclusivity()
    {
        // Arrange - proves the forwarder is not aliased to PutRequired
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .PutOptional(Field.Of<Contact>(x => x.ReportsToId), new DefaultRelationship(new Contact()))
            .RemoveFromMasterTemplate(Field.Of<Contact>(x => x.AccountId))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Assert.Null(bundle.GetList<Contact>(x => x.ReportsToId));
    }

    [Fact]
    public async Task PutOptional_ByLambda_IsSkippedAtRequiredInclusivity()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .PutOptional(x => x.ReportsToId, new DefaultRelationship(new Contact()))
            .RemoveFromMasterTemplate(x => x.AccountId)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Assert.Null(bundle.GetList<Contact>(x => x.ReportsToId));
    }

    [Fact]
    public async Task RemoveFromMasterTemplate_ByPropertyInfo_DropsTheDefault()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .RemoveFromMasterTemplate(Field.Of<Contact>(x => x.Email))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Null(result.Email); // ContactDataProvider defaults Email; the removal took
        Assert.NotNull(result.LastName); // an untouched default is unaffected
    }

    [Fact]
    [SuppressMessage(
        "Performance",
        "HLQ005:Avoid Single() and SingleOrDefault()",
        Justification = "one relationship generated"
    )]
    public async Task IncludeOptional_ByPropertyInfo_PromotesTheOptionalRelationship()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .PutOptional(Field.Of<Contact>(x => x.ReportsToId), new DefaultRelationship(new Contact()))
            .IncludeOptional(Field.Of<Contact>(x => x.ReportsToId))
            .RemoveFromMasterTemplate(Field.Of<Contact>(x => x.AccountId))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        _ = Assert.Single(bundle.GetList<Contact>(x => x.ReportsToId)!);
    }

    [Fact]
    public async Task ExcludeRelationship_ByPropertyInfo_SuppressesTheRequiredRelationship()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .ExcludeRelationship(Field.Of<Contact>(x => x.AccountId))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Assert.Null(bundle.GetList<Contact>(x => x.AccountId));
    }

    [Fact]
    public async Task ExcludeRelationshipIfPresent_IsANoOpWhenTheFieldIsNotARelationship()
    {
        // Arrange - the plain ExcludeRelationship throws here; ExcludeRelationshipIfPresent must not
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .ExcludeRelationshipIfPresent(Field.Of<Contact>(x => x.FirstName))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.NotNull(result.FirstName);
    }

    // Path-scoped ancestor overrides -----------------------------------

    [Fact]
    public async Task Put_ByPath_ForwardsLiteralsAndValueExpressionsToAGeneratedAncestor()
    {
        // Arrange - Site and AccountNumber are untouched by any template, so the path-scoped
        // override is the only thing that could set them on the generated ancestor Account
        List<PropertyInfo> siteOfAccount = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Site)];
        List<PropertyInfo> numberOfAccount =
            [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.AccountNumber)];
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .Put(siteOfAccount, (object?)"HQ")
            .Put(numberOfAccount, new LiteralExpression("AN-42"))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Account generatedAccount = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Equal("HQ", generatedAccount.Site);
        Assert.Equal("AN-42", generatedAccount.AccountNumber);
    }

    [Fact]
    public async Task Put_ByPath_ForwardsAContextAwareExpressionToAGeneratedAncestor()
    {
        // Arrange - the ancestor's BillingCity is copied from its own (path-set) Site
        List<PropertyInfo> siteOfAccount = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Site)];
        List<PropertyInfo> billingCityOfAccount =
            [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.BillingCity)];
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .Put(siteOfAccount, (object?)"Berlin")
            .Put(billingCityOfAccount, CopyFromSiblingExpression.From<Account>(x => x.Site))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Account generatedAccount = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Equal("Berlin", generatedAccount.BillingCity);
    }

    [Fact]
    [SuppressMessage(
        "Performance",
        "HLQ005:Avoid Single() and SingleOrDefault()",
        Justification = "one owner generated"
    )]
    public async Task PutRequired_ByPath_AddsARequiredRelationshipToAGeneratedAncestor()
    {
        // Arrange - the Account under the Contact gets a required Owner it would not otherwise have
        List<PropertyInfo> ownerOfAccount = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)];
        RecordProvider<Contact> provider = new RecordProvider<Contact>(OwnerAwareLookup())
            .PutRequired(ownerOfAccount, new DefaultRelationship(new User()))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Account generatedAccount = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.NotNull(generatedAccount.OwnerId);
    }

    // Children --------------------------------------------------------

    [Fact]
    public async Task WithChildren_GeneratesTheRequestedNumberOfChildren()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 3)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Assert.Equal(3, bundle.GetChildList<Contact>(x => x.AccountId).Count);
    }

    [Fact]
    [SuppressMessage(
        "Performance",
        "HLQ005:Avoid Single() and SingleOrDefault()",
        Justification = "one child requested"
    )]
    public async Task WithChild_GeneratesASingleChild()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .WithChild(Field.Of<Contact>(x => x.AccountId))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        _ = Assert.Single(bundle.GetChildList<Contact>(x => x.AccountId));
    }

    [Fact]
    public async Task With_AcceptsAFullyConfiguredChildProvider()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(ChildProvider.For<Contact>(x => x.AccountId).SetQuantity(2))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Assert.Equal(2, bundle.GetChildList<Contact>(x => x.AccountId).Count);
    }

    // Setters --------------------------------------------------------

    [Fact]
    public async Task SetOverrideTemplateList_GeneratesOneRecordPerTemplate()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .SetOverrideTemplateList([new Contact { FirstName = "Ann" }, new Contact { FirstName = "Bob" }])
            .SetInsertMode(InsertMode.Mock);

        // Act
        List<Contact> results = await provider.SupplyList().ConfigureAwait(true);

        // Assert
        Assert.Equal(["Ann", "Bob"], results.Select(contact => contact.FirstName));
    }

    [Fact]
    public async Task WithVariant_PinsTheProviderVariant()
    {
        // Arrange
        ILookupKey enterprise = FlavouredLookupKey.Get<Account>("enterprise");
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Account>()] = new NamedIndustryAccountProvider("Default"),
            [enterprise] = new NamedIndustryAccountProvider("Enterprise"),
        });
        RecordProvider<Account> provider = new RecordProvider<Account>(lookup)
            .WithVariant(enterprise)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Account result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal("Enterprise", result.Industry);
    }

    [Fact]
    public async Task SetMockIdGenerator_AppliesTheCustomGeneratorToThisCallsPrimary()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .SetMockIdGenerator(new PrefixedIdGenerator("typed"))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.StartsWith("typed-", result.Id);
    }

    [Fact]
    public async Task SetPersistenceGateway_RoutesInsertsThroughTheGivenGateway()
    {
        // Arrange - NSubstitute returns a completed Task for Insert(...) by default
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(gateway);

        // Act
        _ = await provider.Supply().ConfigureAwait(true);

        // Assert
        _ = gateway.Received().Insert(Arg.Any<List<object>>(), Arg.Any<PropertyInfo>());
    }

    [Fact]
    public async Task SetUnsetFieldFiller_RunsTheGivenFillerOverEachGeneratedRecord()
    {
        // Arrange
        IUnsetFieldFiller filler = Substitute.For<IUnsetFieldFiller>();
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .SetUnsetFieldFiller(filler)
            .SetInsertMode(InsertMode.Mock);

        // Act
        _ = await provider.Supply().ConfigureAwait(true);

        // Assert
        filler.Received().Fill(Arg.Any<object>(), Arg.Any<IReadOnlyCollection<PropertyInfo>>());
    }

    [Fact]
    public async Task ExcludePrimaryIds_LeavesThisCallsPrimaryWithoutAnId()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .ExcludePrimaryIds()
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Null(result.Id);
    }

    [Fact]
    public async Task IncludePrimaryIds_UndoesAnEarlierExcludePrimaryIds()
    {
        // Arrange
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .ExcludePrimaryIds()
            .IncludePrimaryIds()
            .SetInsertMode(InsertMode.Mock);

        // Act
        Contact result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.NotNull(result.Id);
    }

    [Fact]
    public async Task TheRemainingChainableSetters_ForwardAndAScenarioStillGenerates()
    {
        // Arrange - AllowAncestorCycles / DepthBatched / ForceStructuralChildGeneration have no
        // isolated observable effect here; this proves each forwards to a real method, returns the
        // wrapper, and leaves a normal generation working.
        RecordProvider<Contact> provider = new RecordProvider<Contact>(Lookup)
            .AllowAncestorCycles()
            .ForceStructuralChildGeneration()
            .SetQuantityPerTemplate(2)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        List<Contact> results = await provider.SupplyList().ConfigureAwait(true);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, contact => Assert.NotNull(contact.Id));
    }

    // Helpers --------------------------------------------------------

    private static IProviderLookup OwnerAwareLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Account>()] = new AccountDataProvider(),
            [LookupKey.Get<Contact>()] = new ContactDataProvider(),
            [LookupKey.Get<User>()] = new PlainUserProvider(),
        });

    private static IProviderLookup DepartmentChildLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Account>()] = new AccountDataProvider(),
            [LookupKey.Get<Contact>()] = new DepartmentedContactProvider(),
        });
}

file abstract class TemplateProvider : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class PlainUserProvider : TemplateProvider
{
    public PlainUserProvider() =>
        this.Template = new MasterTemplate<User>(x => x.Id)
        {
            [x => x.LastName] = new LiteralExpression("Owner"),
        };
}

file sealed class DepartmentedContactProvider : TemplateProvider
{
    public DepartmentedContactProvider() =>
        this.Template = new MasterTemplate<Contact>(x => x.Id)
        {
            [x => x.Department] = new LiteralExpression("Engineering"),
        };
}

file sealed class NamedIndustryAccountProvider : TemplateProvider
{
    public NamedIndustryAccountProvider(string industry) =>
        this.Template = new MasterTemplate<Account>(x => x.Id)
        {
            [x => x.Industry] = new LiteralExpression(industry),
        };
}

file sealed class PrefixedIdGenerator(string prefix) : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"{prefix}-{++this._count}";
}