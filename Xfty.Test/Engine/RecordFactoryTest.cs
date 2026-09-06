using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Engine;

/// <summary>
/// Proves RecordFactory - the engine that turns a Master Template into an
/// object graph - along its two axes: relationship inclusivity (None/
/// Required/All/PreventCascade) and insert mode (Never/Later/Mock). Mock
/// mode proves generation never touches a database, by construction; a real
/// insert under Now is proven in PersistenceGatewayTest.
/// </summary>
public class RecordFactoryTest
{
    private static readonly DefaultProviderLookup DefaultLookup = new();

    private static IProviderLookup LookupOf(Dictionary<ILookupKey, IRecordProvider> providers) => ProviderLookups.Of(providers);

    private static IProviderLookup OptionalChainLookup() =>
        LookupOf(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Contact))] = new OptionalParentContactProvider(),
            [LookupKey.Get(typeof(Account))] = new OptionalOwnerAccountProvider(),
            [LookupKey.Get(typeof(User))] = new LeafUserProvider(),
        });

    // Relationship inclusivity ------------------------------------

    [Fact]
    public async Task SupplyBundle_AtNoneInclusivity_GeneratesNoRelatedRecords()
    {
        // Arrange
        RecordProvider provider = ContactProvider(DefaultLookup).SetInclusivity(InsertInclusivity.None).SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - no Account is generated
        Assert.Null(bundle.GetList<Contact>(x => x.AccountId));
        Assert.Null(((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId);
    }

    [Fact]
    public async Task SupplyBundle_AtRequiredInclusivity_GeneratesTheRequiredParentAndWiresTheLookup()
    {
        // Arrange
        RecordProvider provider = ContactProvider(DefaultLookup).SetInclusivity(InsertInclusivity.Required).SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        List<object> accounts = bundle.GetList<Contact>(x => x.AccountId)!;
        _ = Assert.Single(accounts);
        Assert.Equal(((Account)accounts[0]).Id, ((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId);
    }

    [Fact]
    public async Task SupplyBundle_AtRequiredInclusivity_SkipsAnOptionalRelationship()
    {
        // Arrange
        RecordProvider provider = OptionalParentContactProvider(InsertInclusivity.Required);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - an optional relationship is skipped for Required
        Assert.Null(bundle.GetList<Contact>(x => x.AccountId));
    }

    [Fact]
    public async Task SupplyBundle_AtAllInclusivity_GeneratesAnOptionalRelationship()
    {
        // Arrange
        RecordProvider provider = OptionalParentContactProvider(InsertInclusivity.All);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - an optional relationship is generated for All
        _ = Assert.Single(bundle.GetList<Contact>(x => x.AccountId)!);
    }

    [Fact]
    public async Task SupplyBundle_AtRequiredInclusivity_RecursesIntoTheGrandparent()
    {
        // Arrange
        RecordProvider provider = ContactProvider(DeepChainLookup()).SetInclusivity(InsertInclusivity.Required).SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - Required recurses into the grandparent
        Assert.NotNull(bundle.GetBundle<Contact>(x => x.AccountId)!.GetList<Account>(x => x.OwnerId));
    }

    [Fact]
    public async Task SupplyBundle_AtPreventCascadeInclusivity_GeneratesDirectRelationshipsButStopsRecursing()
    {
        // Arrange
        RecordProvider provider = ContactProvider(DeepChainLookup())
            .SetInclusivity(InsertInclusivity.PreventCascade)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        _ = Assert.Single(bundle.GetList<Contact>(x => x.AccountId)!); // the direct Account is still generated
        Assert.Null(bundle.GetBundle<Contact>(x => x.AccountId)!.GetList<Account>(x => x.OwnerId)); // PreventCascade stops the second level generating its own relationships
    }

    [Fact]
    public async Task SupplyBundle_WhenARelationshipCopiesANonIdField_WiresItFromTheGeneratedParent()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(
            typeof(Contact),
            LookupOf(new Dictionary<ILookupKey, IRecordProvider>
            {
                [LookupKey.Get(typeof(Contact))] = new RelatedFieldContactProvider(),
                [LookupKey.Get(typeof(Account))] = new LeafAccountProvider(),
            }))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Never);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - Contact.Description is copied from the parent Account.Name
        Assert.Equal("Wired From Parent", ((Contact)bundle.GetList<Contact>(x => x.Id)![0]).Department);
    }

    [Fact]
    public async Task SupplyBundle_WhenALookupFieldIsAlreadySetOnTheChildTemplate_DoesNotOverwriteIt()
    {
        // Arrange
        string presetAccountId = IdMocker.GenerateId();
        RecordProvider provider = new RecordProvider(typeof(Contact), DefaultLookup)
            .SetOverrideTemplate(new Contact { LastName = "Preset", AccountId = presetAccountId })
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Never);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        Assert.Equal(presetAccountId, ((Contact)bundle.GetList<Contact>(x => x.Id)![0]).AccountId); // the preset lookup value is kept
        Assert.NotNull(bundle.GetList<Contact>(x => x.AccountId)); // the Account is still generated into the bundle
    }

    [Fact]
    public async Task SupplyBundle_WithQuantity_GeneratesADistinctParentPerRecordEachWired()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), DefaultLookup)
            .SetQuantityPerTemplate(3)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        List<Contact> contacts = [.. bundle.GetList<Contact>(x => x.Id)!.Cast<Contact>()];
        List<Account> accounts = [.. bundle.GetList<Contact>(x => x.AccountId)!.Cast<Account>()];
        Assert.Equal(3, contacts.Count);
        Assert.Equal(3, accounts.Count);
        HashSet<string?> accountIds = [];
        for (int i = 0; i < 3; i++)
        {
            _ = accountIds.Add(accounts[i].Id);
            Assert.Equal(accounts[i].Id, contacts[i].AccountId); // row i wired to its own parent
        }

        Assert.Equal(3, accountIds.Count); // each Contact gets a distinct Account
    }

    // Insert modes ---------------------------------------------

    [Fact]
    public Task SupplyBundle_InNeverMode_LeavesEveryRecordWithoutAnId() => AssertNoIdsGenerated(InsertMode.Never);

    [Fact]
    public Task SupplyBundle_InLaterMode_LeavesEveryRecordWithoutAnId() => AssertNoIdsGenerated(InsertMode.Later);

    [Fact]
    public async Task SupplyBundle_InMockMode_AssignsIdsToEveryRecord()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), DefaultLookup)
            .SetOverrideTemplate(new Contact { LastName = "Factory Test" })
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        Assert.NotNull(((Contact)bundle.GetList<Contact>(x => x.Id)![0]).Id);
        Assert.NotNull(((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Id);
    }

    // IncludeOptional ----------------------------------------

    [Fact]
    public async Task IncludeOptional_ForAOneStepPath_ForcesThatOptionalRelationshipButNoDeeper()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), OptionalChainLookup())
            .IncludeOptional([Field.Of<Contact>(x => x.AccountId)])
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        _ = Assert.Single(bundle.GetList<Contact>(x => x.AccountId)!); // the optional Account is forced
        Assert.Null(bundle.GetBundle<Contact>(x => x.AccountId)!.GetList<Account>(x => x.OwnerId)); // but not the Account Owner - the path stopped at one step
    }

    [Fact]
    public async Task IncludeOptional_ForAMultiStepPath_ForcesEveryStep()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), OptionalChainLookup())
            .IncludeOptional([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)])
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        _ = Assert.Single(bundle.GetList<Contact>(x => x.AccountId)!); // the optional Account is forced
        _ = Assert.Single(bundle.GetBundle<Contact>(x => x.AccountId)!.GetList<Account>(x => x.OwnerId)!); // and its optional Owner, one step deeper
    }

    [Fact]
    public async Task SupplyBundle_WithoutIncludeOptional_LeavesTheOptionalChainUngenerated()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), OptionalChainLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert - optional relationship skipped under Required
        Assert.Null(bundle.GetList<Contact>(x => x.AccountId));
    }

    [Fact]
    public async Task IncludeOptional_WhenAStepIsNotARelationship_Throws()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), OptionalChainLookup())
            .IncludeOptional([Field.Of<Contact>(x => x.FirstName)])
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        XftyConfigurationException thrown = await Assert.ThrowsAsync<XftyConfigurationException>(provider.SupplyBundle);

        // Assert
        Assert.Contains("not a relationship", thrown.Message);
    }

    [Fact]
    public void IncludeOptional_WhenGivenAnEmptyPath_Throws() => AssertIncludeOptionalRejects([], "at least one non-null relationship");

    [Fact]
    public void IncludeOptional_WhenGivenANullStep_Throws() =>
        AssertIncludeOptionalRejects([Field.Of<Contact>(x => x.AccountId), null!], "at least one non-null relationship");

    // Runners + helpers -------------------------------------

    private static RecordProvider ContactProvider(IProviderLookup lookup) =>
        new RecordProvider(typeof(Contact), lookup).SetOverrideTemplate(new Contact { LastName = "Factory Test" });

    private static IProviderLookup DeepChainLookup() =>
        LookupOf(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Contact))] = new DeepContactProvider(),
            [LookupKey.Get(typeof(Account))] = new DeepAccountProvider(),
            [LookupKey.Get(typeof(User))] = new LeafUserProvider(),
        });

    private static RecordProvider OptionalParentContactProvider(InsertInclusivity inclusivity) =>
        ContactProvider(LookupOf(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Contact))] = new OptionalParentContactProvider(),
            [LookupKey.Get(typeof(Account))] = new LeafAccountProvider(),
        })).SetInclusivity(inclusivity).SetInsertMode(InsertMode.Mock);

    private static async Task AssertNoIdsGenerated(InsertMode insertMode)
    {
        // Arrange
        RecordProvider provider = ContactProvider(DefaultLookup).SetInclusivity(InsertInclusivity.Required).SetInsertMode(insertMode);

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        Assert.Null(((Contact)bundle.GetList<Contact>(x => x.Id)![0]).Id);
        Assert.Null(((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Id);
    }

    private static void AssertIncludeOptionalRejects(List<PropertyInfo> path, string expectedMessagePart)
    {
        // Arrange
        RecordProvider provider = new(typeof(Contact), DefaultLookup);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => provider.IncludeOptional(path));

        // Assert
        Assert.Contains(expectedMessagePart, thrown.Message);
    }
}

file abstract class BaseProvider : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class LeafAccountProvider : BaseProvider
{
    public LeafAccountProvider() =>
        this.Template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new IncrementingStringExpression("Leaf Account"));
}

file sealed class OptionalParentContactProvider : BaseProvider
{
    public OptionalParentContactProvider() =>
        this.Template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .Put<Contact>(x => x.LastName, new IncrementingStringExpression("Optional Parent Contact"))
            .PutOptional<Contact>(x => x.AccountId, new DefaultRelationship(new Account()));
}

file sealed class DeepAccountProvider : BaseProvider
{
    public DeepAccountProvider() =>
        this.Template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new IncrementingStringExpression("Deep Account"))
            .PutRequired<Account>(x => x.OwnerId, new DefaultRelationship(new User()));
}

file sealed class DeepContactProvider : BaseProvider
{
    public DeepContactProvider() =>
        this.Template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .Put<Contact>(x => x.LastName, new IncrementingStringExpression("Deep Contact"))
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account()));
}

/// <summary>Copies the parent Account's Name onto Contact.Department via a related-field relationship (a writable stand-in - Contact.Description isn't settable on this demo type).</summary>
file sealed class RelatedFieldContactProvider : BaseProvider
{
    public RelatedFieldContactProvider() =>
        this.Template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .Put<Contact>(x => x.LastName, new IncrementingStringExpression("Related Field Contact"))
            .PutRequired(
                Field.Of<Contact>(x => x.Department),
                new DefaultRelationship(new Account { Name = "Wired From Parent" }, Field.Of<Account>(x => x.Name)));
}

file sealed class OptionalOwnerAccountProvider : BaseProvider
{
    public OptionalOwnerAccountProvider() =>
        this.Template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new IncrementingStringExpression("Opt Owner Account"))
            .PutOptional<Account>(x => x.OwnerId, new DefaultRelationship(new User()));
}

file sealed class LeafUserProvider : BaseProvider
{
    public LeafUserProvider() =>
        this.Template = new MasterTemplate(Field.Of<User>(x => x.Id))
            .Put<User>(x => x.LastName, new IncrementingStringExpression("User"));
}
