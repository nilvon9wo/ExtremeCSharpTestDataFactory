using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Enrichment;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Enrichment;

/// <summary>
/// Proves BundleEnricher end to end - a generated graph re-expressed in the
/// shape an init-only property rejects. Mock generation only, no persistence.
/// </summary>
public class BundleEnricherTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountWithParentProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
            [LookupKey.Get(typeof(Case))] = new CaseProvider(),
        });

    [Fact]
    public async Task InjectAll_ForThePrimary_GraftsTheGeneratedAncestorOntoEveryRecord()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetQuantityPerTemplate(2)
            .SupplyBundle();

        // Act
        List<object> enriched = bundle.InjectAll(Field.Of<Contact>(x => x.Id));

        // Assert
        List<Contact> contacts = [.. enriched.Cast<Contact>()];
        Assert.NotNull(contacts[0].Account); // row 0 Account grafted
        Assert.NotNull(contacts[1].Account!.Name); // row 1 Account grafted with fields
    }

    [Fact]
    public async Task Inject_WithParentDepthTwo_GraftsTheWholeAncestorChain()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .IncludeOptional([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)])
            .AllowAncestorCycles()
            .SupplyBundle();
        InjectConfig config = InjectConfig.AllParents();

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        // Assert
        Contact enrichedContact = (Contact)enriched[0];
        Assert.NotNull(enrichedContact.Account!.Parent); // the grandparent Account is on the chain
    }

    [Fact]
    public async Task Inject_WithParentDepthOne_StopsBeforeTheGrandparent()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .IncludeOptional([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)])
            .AllowAncestorCycles()
            .SupplyBundle();
        InjectConfig config = InjectConfig.AllParents().ParentDepth(1);

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        // Assert
        Contact enrichedContact = (Contact)enriched[0];
        Assert.NotNull(enrichedContact.Account); // depth 1 still grafts the Account
        Assert.Null(enrichedContact.Account!.Parent); // depth 1 stops before the grandparent
    }

    [Fact]
    public async Task InjectAllChildren_ForThePrimary_GraftsTheChildSubquery()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 3)
            .SupplyBundle();

        // Act
        List<object> enriched = bundle.InjectAllChildren(Field.Of<Account>(x => x.Id));

        // Assert
        Account enrichedAccount = (Account)enriched[0];
        Assert.Equal(3, enrichedAccount.Contacts!.Count);
    }

    [Fact]
    public async Task InjectAll_ForAGeneratedAncestorField_GraftsTheInverseChildren()
    {
        // Arrange - two Contacts, each generates its own Account
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetQuantityPerTemplate(2)
            .SupplyBundle();

        // Act
        List<object> enriched = bundle.InjectAll(Field.Of<Contact>(x => x.AccountId));

        // Assert
        Account firstAccount = (Account)enriched[0];
        _ = Assert.Single(firstAccount.Contacts!); // the Account carries the Contact that generated it
    }

    [Fact]
    public async Task Inject_WithInjectValueOnTheRecord_ForcesTheScalar()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();
        DateTime forced = new(2020, 1, 1, 0, 0, 0);
        InjectConfig config = InjectConfig.Nothing().InjectValue(Field.Of<Contact>(x => x.Birthdate), forced);

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        // Assert
        Assert.Equal(forced, ((Contact)enriched[0]).Birthdate);
    }

    [Fact]
    public async Task Inject_WithAnAncestorPathValue_ForcesAScalarSeveralHopsUp()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();
        InjectConfig config = InjectConfig.Nothing()
            .InjectValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.AnnualRevenue)], 9999m);

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        // Assert
        Assert.Equal(9999m, ((Contact)enriched[0]).Account!.AnnualRevenue);
    }

    [Fact]
    public async Task Inject_WithExcludeParent_SkipsThatAncestor()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();
        InjectConfig config = InjectConfig.Everything().ExcludeParent([Field.Of<Contact>(x => x.AccountId)]);

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        // Assert
        Assert.Null(((Contact)enriched[0]).Account); // the excluded ancestor was not grafted
    }

    [Fact]
    public async Task InjectAll_ForAFieldTheBundleDoesNotHold_Throws()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.None)
            .SupplyBundle();

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => bundle.InjectAll(Field.Of<Contact>(x => x.Id)));

        // Assert - nothing generated, InjectAll has nothing to inject
        Assert.NotNull(thrown);
    }

    [Fact]
    public async Task Inject_Always_LeavesTheOriginalBundleUntouched()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();

        // Act
        _ = bundle.InjectAll(Field.Of<Contact>(x => x.Id));

        // Assert - the source record was not mutated
        Assert.Null(((Contact)bundle.PrimaryRecords()![0]).Account);
    }

    [Fact]
    public async Task Inject_WhenAllowDeeperGraphIsNotSet_RejectsAnOverDeepParentDepth()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();
        InjectConfig config = InjectConfig.AllParents().ParentDepth(9);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => bundle.Inject(Field.Of<Contact>(x => x.Id), config));

        // Assert - the error points at the escape hatch
        Assert.Contains("AllowDeeperGraph", thrown.Message);
    }

    [Fact]
    public async Task InjectAll_ForThePrimary_AlsoPutsTheAncestorsInverseChildOntoIt()
    {
        // Arrange - one Contact; InjectAll should give it contact.Account and contact.Account.Contacts
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();

        // Act
        List<object> enriched = bundle.InjectAll(Field.Of<Contact>(x => x.Id));

        // Assert
        Contact enrichedContact = (Contact)enriched[0];
        _ = Assert.Single(enrichedContact.Account!.Contacts!); // the ancestor carries the Contact that generated it
    }

    [Fact]
    public async Task InjectAll_ForThePrimary_GivesEachDownwardChildItsOwnAncestor()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 2)
            .SupplyBundle();

        // Act - Everything() covers ancestors at every position, including inside a subquery
        List<object> enriched = bundle.InjectAll(Field.Of<Account>(x => x.Id));

        // Assert
        Assert.NotNull(((Account)enriched[0]).Contacts![0].Account); // the child Contact carries its generated Account
    }

    [Fact]
    public async Task Inject_WithChildDepthTwoAndAllowDeeperGraph_GraftsGrandchildren()
    {
        // Arrange - Account -> 2 Contacts -> 3 Cases each
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(ChildProvider.For<Contact>(x => x.AccountId).SetQuantity(2)
                .With(ChildProvider.For<Case>(x => x.ContactId).SetQuantity(3)))
            .SupplyBundle();
        InjectConfig config = InjectConfig.AllChildren().ChildDepth(2).AllowDeeperGraph();

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Account>(x => x.Id), config);

        // Assert - the nested subquery is grafted
        Assert.Equal(3, ((Account)enriched[0]).Contacts![0].Cases!.Count);
    }

    [Fact]
    public async Task Inject_WithChildDepthTwoButNoAllowDeeperGraph_Throws()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 1)
            .SupplyBundle();
        InjectConfig config = InjectConfig.AllChildren().ChildDepth(2);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => bundle.Inject(Field.Of<Account>(x => x.Id), config));

        // Assert
        Assert.NotNull(thrown);
    }

    [Fact]
    public async Task Inject_WithInjectChildValue_ForcesTheScalarOnEveryChild()
    {
        // Arrange - Account with 3 Contacts
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 3)
            .SupplyBundle();
        InjectConfig config = InjectConfig.Nothing()
            .InjectChildValue(Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Birthdate), new DateTime(2021, 6, 1));

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Account>(x => x.Id), config);

        // Assert
        List<Contact> children = ((Account)enriched[0]).Contacts!;
        Assert.Equal(new DateTime(2021, 6, 1), children[0].Birthdate);
        Assert.Equal(new DateTime(2021, 6, 1), children[2].Birthdate);
    }

    [Fact]
    public async Task Inject_WithInjectChildValueExpression_GivesEachChildADistinctValue()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 3)
            .SupplyBundle();
        InjectConfig config = InjectConfig.Nothing().InjectChildValue(
            Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department), new IncrementingStringExpression("note"));

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Account>(x => x.Id), config);

        // Assert
        List<Contact> children = ((Account)enriched[0]).Contacts!;
        HashSet<string?> distinct = [children[0].Department, children[1].Department, children[2].Department];
        Assert.Equal(3, distinct.Count); // the expression resolved fresh for each child
    }

    [Fact]
    public void Inject_OnASelfHierarchy_ChildValueTargetsTheChildrenAndPathValueTheParent()
    {
        // Arrange - a middle Account, one parent Account and two child Accounts, all via Account.ParentId
        Bundle parentSub = new();
        parentSub.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account { Name = "original parent" }]);
        Bundle childrenSub = new();
        childrenSub.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account { Name = "original c0" }, new Account { Name = "original c1" }]);
        Bundle middle = new();
        middle.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account { Name = "Middle" }]);
        _ = middle.Put<Account>(x => x.ParentId, parentSub.PrimaryRecords()!);
        _ = middle.Put<Account>(x => x.ParentId, parentSub);
        _ = middle.PutChild(Field.Of<Account>(x => x.ParentId), childrenSub, [0, 0]);
        InjectConfig config = InjectConfig.Nothing()
            .InjectValue([Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.Name)], "parent name")
            .InjectChildValue(Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.Name), "child name");

        // Act
        List<object> enriched = middle.Inject(Field.Of<Account>(x => x.Id), config);

        // Assert
        Account enrichedMiddle = (Account)enriched[0];
        Assert.Equal("parent name", enrichedMiddle.Parent!.Name); // InjectValue(path) walked up to the parent
        Assert.Equal("child name", enrichedMiddle.ChildAccounts![0].Name); // InjectChildValue walked down to the children
        Assert.Equal("child name", enrichedMiddle.ChildAccounts![1].Name);
    }

    [Fact]
    public async Task Inject_WithAnInjectChildValuePathTheGraphNeverProduced_Throws()
    {
        // Arrange - no Cases generated, so the InjectChildValue path is never reached
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 1)
            .SupplyBundle();
        InjectConfig config = InjectConfig.Nothing().InjectChildValue(Field.Of<Case>(x => x.ContactId), Field.Of<Case>(x => x.Subject), "x");

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => bundle.Inject(Field.Of<Account>(x => x.Id), config));

        // Assert - the error names the unreached path
        Assert.Contains("InjectChildValue", thrown.Message);
    }

    [Fact]
    public async Task Inject_WithAnInjectChildValueDeeperThanChildDepth_Throws()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 1)
            .SupplyBundle();
        InjectConfig config = InjectConfig.Nothing().InjectChildValue(
            [Field.Of<Contact>(x => x.AccountId), Field.Of<Case>(x => x.ContactId), Field.Of<Case>(x => x.Subject)], "x");

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => bundle.Inject(Field.Of<Account>(x => x.Id), config));

        // Assert - the error points at childDepth
        Assert.Contains("childDepth", thrown.Message);
    }

    [Fact]
    public async Task Inject_ReturnsThePlainListOfEnrichedTargetRecords()
    {
        // Arrange
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetQuantityPerTemplate(2)
            .SupplyBundle();

        // Act
        List<object> enriched = bundle.Inject(Field.Of<Contact>(x => x.Id), InjectConfig.AllParents());

        // Assert
        Assert.Equal(2, enriched.Count);
        _ = Assert.IsType<Contact>(enriched[0]);
    }
}

file sealed class CaseProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Case>(x => x.Id))
        .Put<Case>(x => x.Subject, new IncrementingStringExpression("Enricher Case"));

    public PropertyInfo PrimaryTargetField => Field.Of<Case>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}

file sealed class AccountWithParentProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Account>(x => x.Id))
        .Put<Account>(x => x.Name, new IncrementingStringExpression("Enricher Account"))
        .PutOptional<Account>(x => x.ParentId, new DefaultRelationship(new Account { Name = "Parent Co" }));

    public PropertyInfo PrimaryTargetField => Field.Of<Account>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
