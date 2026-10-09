using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Relationships;

/// <summary>
/// Proves <see cref="SharedAncestorProvider"/>'s configuration surface - the
/// value, relationship and path puts a shared ancestor takes, and how its
/// record template is chosen. The shared record is a Contact (whose own
/// required Account, and that Account's owning User, give the path puts
/// somewhere to land); each test's Case references it.
///
/// Resets the process-wide registry around every test: one test registers an
/// ancestor that can never resolve, which every later pre-phase would trip on.
/// </summary>
public sealed class SharedAncestorProviderTest : IDisposable
{
    private const string Name = "shared-ancestor-provider-test";
    private const string Marker = "Set by the shared ancestor";

    private static readonly PropertyInfo ContactsAccount = Field.Of<Contact>(x => x.AccountId);

    public SharedAncestorProviderTest() => SharedAncestor.ResetAllForTesting();

    public void Dispose() => SharedAncestor.ResetAllForTesting();

    // Values -----------------------------------------------------------

    [Fact]
    public async Task Put_WithAValueExpression_SetsTheFieldOnTheSharedRecord()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, new Contact())
            .Put(Field.Of<Contact>(x => x.Department), new LiteralExpression(Marker));

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.Equal(Marker, SharedContact().Department);
    }

    [Fact]
    public async Task Put_ByLambdaWithAValueExpression_SetsTheFieldOnTheSharedRecord()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, new Contact())
            .Put<Contact>(x => x.Department, new LiteralExpression(Marker));

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.Equal(Marker, SharedContact().Department);
    }

    // Optional relationships -------------------------------------------

    [Fact]
    public async Task PutOptional_WithIncludeOptional_GeneratesTheOptionalParentFromItsTemplate()
    {
        // Arrange - optional, so only IncludeOptional generates it under the default Required inclusivity
        _ = SharedAncestor.Put(Name, new Contact())
            .PutOptional<Contact>(x => x.AccountId, new DefaultRelationship(new Account { Name = Marker }))
            .IncludeOptional<Contact>(x => x.AccountId);

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.Equal(Marker, SharedAccount().Name);
    }

    // Path puts ---------------------------------------------------------

    [Fact]
    public async Task Put_ByPathWithAValueExpression_SetsTheFieldOnTheSharedRecordsAncestor()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, new Contact())
            .Put([ContactsAccount, Field.Of<Account>(x => x.Site)], new LiteralExpression(Marker));

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.Equal(Marker, SharedAccount().Site);
    }

    [Fact]
    public async Task Put_ByPathWithAContextAwareExpression_EvaluatesItOnTheSharedRecordsAncestor()
    {
        // Arrange - the ancestor's BillingCity is copied from its own template-set ShippingCity
        _ = SharedAncestor.Put(Name, new Contact())
            .Put(
                [ContactsAccount, Field.Of<Account>(x => x.BillingCity)],
                CopyFromSiblingExpression.From<Account>(x => x.ShippingCity)
            );

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.Equal(AccountDataProvider.DefaultShippingCity, SharedAccount().BillingCity);
    }

    [Fact]
    public async Task Put_ByPathWithALiteral_SetsTheFieldOnTheSharedRecordsAncestor()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, new Contact())
            .Put([ContactsAccount, Field.Of<Account>(x => x.AccountNumber)], Marker);

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.Equal(Marker, SharedAccount().AccountNumber);
    }

    [Fact]
    public async Task PutRequired_ByPath_GivesTheSharedRecordsAncestorItsOwnParent()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, new Contact())
            .PutRequired([ContactsAccount, Field.Of<Account>(x => x.OwnerId)], new DefaultRelationship(new User()));

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.NotNull(SharedAccount().OwnerId);
    }

    [Fact]
    public async Task PutOptional_ByPath_GivesTheSharedRecordsAncestorItsOwnParentWhenOptionalsAreIncluded()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, new Contact())
            .PutOptional([ContactsAccount, Field.Of<Account>(x => x.OwnerId)], new DefaultRelationship(new User()))
            .SetInclusivity(InsertInclusivity.All);

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert
        Assert.NotNull(SharedAccount().OwnerId);
    }

    // Record template ---------------------------------------------------

    [Fact]
    public async Task Put_ByVariantKey_GeneratesTheSharedRecordFromABlankTemplate()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, LookupKey.Get<Contact>());

        // Act
        await SupplyACaseUnderTheSharedContact().ConfigureAwait(true);

        // Assert - every field came from the variant's Provider, none from a template
        Assert.StartsWith(ContactDataProvider.DefaultLastNamePrefix, SharedContact().LastName);
    }

    [Fact]
    public async Task Supply_WhenTheSharedAncestorHasNeitherTemplateNorVariant_Throws()
    {
        // Arrange
        _ = SharedAncestor.Put(Name, (object?)null);

        // Act
        XftyConfigurationException thrown = await Assert.ThrowsAsync<XftyConfigurationException>(
            SupplyACaseUnderTheSharedContact
        ).ConfigureAwait(true);

        // Assert
        Assert.Contains("needs SharedAncestor.PutAsTemplate(...) or Put(name, key)", thrown.Message);
    }

    // Helpers -----------------------------------------------------------

    private static IProviderLocating Lookup() =>
        ProviderLookups.Of(new Dictionary<IRecordIdentifying, IRecordProviding>
        {
            [LookupKey.Get<Account>()] = new AccountDataProvider(),
            [LookupKey.Get<Contact>()] = new ContactDataProvider(),
            [LookupKey.Get<Case>()] = new CaseUnderContactProvider(),
            [LookupKey.Get<User>()] = new OwnerProvider(),
        });

    private static async Task SupplyACaseUnderTheSharedContact() =>
        _ = await new RecordProvider(typeof(Case), Lookup())
            .PutRequired<Case>(x => x.ContactId, SharedAncestor.Get(Name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyBundle().ConfigureAwait(false);

    private static Bundle SharedBundle() => SharedAncestor.Get(Name).GetResolvedBundle();

    private static Contact SharedContact() => (Contact)SharedBundle().PrimaryRecords()![0];

    private static Account SharedAccount() => (Account)SharedBundle().GetList<Contact>(x => x.AccountId)![0];
}

file sealed class CaseUnderContactProvider : IRecordProviding
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate(Field.Of<Case>(x => x.Id))
        .Put<Case>(x => x.Subject, new IncrementingStringExpression("Case"));

    public PropertyInfo PrimaryTargetField => Field.Of<Case>(x => x.Id);

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}

file sealed class OwnerProvider : IRecordProviding
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate(Field.Of<User>(x => x.Id))
        .Put<User>(x => x.LastName, new LiteralExpression("Owner"));

    public PropertyInfo PrimaryTargetField => Field.Of<User>(x => x.Id);

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}