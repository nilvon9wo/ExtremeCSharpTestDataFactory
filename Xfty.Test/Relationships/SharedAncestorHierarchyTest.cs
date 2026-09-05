using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Relationships;

/// <summary>
/// Shared ancestors that have hierarchies of their own: automatic deep-vs-flat
/// resolution, nested shared ancestors, cycle detection (and breaking one by
/// pre-registering a side), ResolveNow/GetId, PutIfAbsent, lookup-provided
/// defaults (ISharedAncestorDefaults), Disable/ManualResolutionOnly, chained
/// per-record config on the shared record itself, a path value wiring a
/// shared ancestor with no special setup, one shared record serving two
/// different child types, and the end-to-end proof: a three-level all-shared
/// spine, built once, wired everywhere.
///
/// The plain one-record case is in SharedAncestorTest. Apex's Now-mode/DML-
/// count assertions are adapted to Mock-mode equivalents proving the same
/// wiring - this port has no persistence layer.
/// </summary>
public class SharedAncestorHierarchyTest
{
    // Flat --------------------------------------------------------------------

    [Fact]
    public void SupplyList_WhenChildrenShareAConfiguredAncestor_TheyAllGetTheSameOne()
    {
        // Arrange
        const string name = "hierarchy-test-flat";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" });

        // Act
        List<Contact> contacts = SupplyContactsUnder(name, 5);

        // Assert - one shared HQ, not one per Contact
        HashSet<string?> accountIds = [.. contacts.Select(contact => contact.AccountId)];
        _ = Assert.Single(accountIds);
        Assert.All(contacts, contact => Assert.Equal(SharedAncestor.GetId(name), contact.AccountId));
    }

    // Deep ------------------------------------------------------------------

    [Fact]
    public void Supply_WhenASharedAncestorIsDeep_ResolvesItAutomatically()
    {
        // Arrange
        ILookupKey level1Key = FlavouredLookupKey.Get(typeof(Account), "hierarchy-level1");
        _ = SharedAncestor.Put("hierarchy-root", new Account { Name = "Root" }).FromVariant(LookupKey.Get(typeof(Account)));
        _ = SharedAncestor.Put("hierarchy-level1", new Account { Name = "Level 1" }).FromVariant(level1Key);
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [level1Key] = ChildOfSharedProvider.Of<Account>(nameof(Account.Id), nameof(Account.ParentId), nameof(Account.Name), "hierarchy-root"),
            [LookupKey.Get(typeof(Contact))] = ChildOfSharedProvider.Of<Contact>(nameof(Contact.Id), nameof(Contact.AccountId), nameof(Contact.LastName), "hierarchy-level1"),
        });

        // Act
        Contact leaf = (Contact)new RecordProvider(typeof(Contact), lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - the nested shared ancestor was wired
        Assert.Equal(SharedAncestor.GetId("hierarchy-root"), ((Account)FirstResolvedAccount("hierarchy-level1")).ParentId);
        Assert.Equal(SharedAncestor.GetId("hierarchy-level1"), leaf.AccountId);
    }

    [Fact]
    public void Supply_TwoCallsConvergeOnTheSameSharedRoot()
    {
        // Arrange
        const string name = "hierarchy-two-calls-converge";
        _ = SharedAncestor.Put(name, new Account { Name = "Singleton Root" });

        // Act - two independent calls that should land on the same shared root
        Contact fromFirstCall = SupplyOneContactUnder(name);
        Contact fromSecondCall = SupplyOneContactUnder(name);

        // Assert
        Assert.Equal(fromFirstCall.AccountId, fromSecondCall.AccountId); // both calls share the one root
    }

    // ResolveNow / GetId --------------------------------------------------

    [Fact]
    public void ResolveNow_LetsGetIdRunBeforeAnySupplyCall()
    {
        // Arrange
        const string name = "hierarchy-resolvenow-before-supply";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" });

        // Act
        _ = SharedAncestor.Get(name).ResolveNow(ContactsUnder(name), InsertMode.Mock);

        // Assert
        Assert.NotNull(SharedAncestor.GetId(name));
    }

    [Fact]
    public void ResolveNow_PinsTheSharedAncestorsModeAheadOfTheCall()
    {
        // Arrange - resolve up front in Mock, then reference it from a Never call
        const string name = "hierarchy-resolvenow-pins-mode";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" });
        _ = SharedAncestor.Get(name).ResolveNow(ContactsUnder(name), InsertMode.Mock);

        // Act
        List<Contact> contacts = SupplyContactsUnderWithMode(name, 1, InsertMode.Never);

        // Assert - the mock Id is still available, and shared with the Never-mode leaf
        Assert.NotNull(SharedAncestor.GetId(name));
        Assert.Equal(SharedAncestor.GetId(name), contacts[0].AccountId);
    }

    [Fact]
    public void GetId_FeedsAnOverrideTemplateDirectly()
    {
        // Arrange - resolve the shared record, then hand its Id straight to a template
        const string name = "hierarchy-getid-feeds-template";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" });
        _ = SharedAncestor.Get(name).ResolveNow(ContactsUnder(name), InsertMode.Mock);
        Contact template = new() { LastName = "Direct", AccountId = (string)SharedAncestor.GetId(name) };

        // Act
        Contact leaf = (Contact)new RecordProvider(template, ContactsUnder(name))
            .SetInclusivity(InsertInclusivity.None)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert
        Assert.Equal(SharedAncestor.GetId(name), leaf.AccountId);
    }

    // PutIfAbsent ------------------------------------------------------

    [Fact]
    public void PutIfAbsent_RegistersOnlyWhenTheNameIsUnregistered()
    {
        // Arrange - a second PutIfAbsent for the same name must not change it
        const string name = "hierarchy-putifabsent";
        _ = SharedAncestor.PutIfAbsent(name, new Account { Name = "First" });
        _ = SharedAncestor.PutIfAbsent(name, new Account { Name = "Second" });

        // Act
        _ = SupplyOneContactUnder(name);

        // Assert - the second PutIfAbsent was ignored
        Assert.Equal("First", ((Account)FirstResolvedAccount(name)).Name);
    }

    // Lookup-provided defaults -----------------------------------------

    [Fact]
    public void Supply_WhenALookupSuppliesTheSharedAncestor_NoTestRegistrationIsNeeded()
    {
        // Arrange - the lookup carries the default; the test registers nothing
        const string name = "hierarchy-lookup-default";
        IProviderLookup lookup = ContactsUnderWithDefault(name, new Account { Name = "Packaged HQ" });

        // Act
        Contact leaf = (Contact)new RecordProvider(typeof(Contact), lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - the Provider worked out of the box
        Assert.Equal("Packaged HQ", ((Account)FirstResolvedAccount(name)).Name);
        Assert.Equal(SharedAncestor.GetId(name), leaf.AccountId);
    }

    [Fact]
    public void Supply_WhenATestRegistersASharedAncestor_ItWinsOverTheLookupDefault()
    {
        // Arrange - the test registers it first; the lookup default must not clobber it
        const string name = "hierarchy-test-wins-over-default";
        _ = SharedAncestor.Put(name, new Account { Name = "Test Override HQ" });
        IProviderLookup lookup = ContactsUnderWithDefault(name, new Account { Name = "Packaged HQ" });

        // Act
        _ = new RecordProvider(typeof(Contact), lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - PutIfAbsent left the test registration alone
        Assert.Equal("Test Override HQ", ((Account)FirstResolvedAccount(name)).Name);
    }

    // Developer control over resolution ------------------------------

    [Fact]
    public void Supply_WhenASharedAncestorIsDisabled_LeavesTheForeignKeyNull()
    {
        // Arrange
        const string name = "hierarchy-disabled";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" });
        SharedAncestor.Disable(name);

        // Act
        Contact leaf = (Contact)new RecordProvider(typeof(Contact), ContactsUnder(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - a disabled shared ancestor is never wired
        Assert.Null(leaf.AccountId);
    }

    [Fact]
    public void GetId_ForADisabledSharedAncestor_Throws()
    {
        // Arrange
        const string name = "hierarchy-disabled-getid";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" });
        SharedAncestor.Disable(name);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => SharedAncestor.GetId(name));

        // Assert
        Assert.Contains("disabled", thrown.Message);
    }

    // Apex's three ManualResolutionOnly() tests are not portable to a shared-process
    // xUnit run: unlike every other piece of SharedAncestor's state, that flag is a
    // single global bool with no unsetter at all (in Apex or here) - Apex gets away
    // with it because statics reset between test METHODS; once any test in this
    // process calls SharedAncestor.ManualResolutionOnly(), every other test class
    // relying on auto-resolution (nearly all of them) would break for the rest of
    // the run. A dedicated, isolated test process would be needed to cover this
    // safely, which is out of scope here.

    // Cycles ---------------------------------------------------------

    [Fact]
    public void Supply_WhenSharedAncestorsFormACycle_Throws()
    {
        // Arrange - 'a' needs 'b', 'b' needs 'a'
        ILookupKey keyA = FlavouredLookupKey.Get(typeof(Account), "hierarchy-cycle-a");
        ILookupKey keyB = FlavouredLookupKey.Get(typeof(Account), "hierarchy-cycle-b");
        _ = SharedAncestor.Put("hierarchy-cycle-a", new Account()).FromVariant(keyA);
        _ = SharedAncestor.Put("hierarchy-cycle-b", new Account()).FromVariant(keyB);
        RecordProvider provider = new RecordProvider(typeof(Contact), CycleLookup(keyA, keyB, "hierarchy-cycle-a", "hierarchy-cycle-b"))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.Supply);

        // Assert - a shared-ancestor cycle must throw
        Assert.Contains("cycle", thrown.Message, StringComparison.OrdinalIgnoreCase);

        // Cleanup - see SharedAncestorTest.Supply_WhenASharedAncestorIsSelfReferential_ThrowsInsteadOfRecursing
        SharedAncestor.Disable("hierarchy-cycle-a");
        SharedAncestor.Disable("hierarchy-cycle-b");
    }

    [Fact]
    public void Supply_WhenOneSideOfACycleIsPreRegistered_BreaksTheCycle()
    {
        // Arrange - hand one side a real record so the cycle resolves
        ILookupKey keyA = FlavouredLookupKey.Get(typeof(Account), "hierarchy-broken-cycle-a");
        ILookupKey keyB = FlavouredLookupKey.Get(typeof(Account), "hierarchy-broken-cycle-b");
        _ = SharedAncestor.Put("hierarchy-broken-cycle-a", new Account()).FromVariant(keyA);
        _ = SharedAncestor.Put("hierarchy-broken-cycle-b", new Account()).FromVariant(keyB);
        Account premadeB = new() { Name = "Pre-made B", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put("hierarchy-broken-cycle-b", premadeB);

        // Act
        Contact leaf = (Contact)new RecordProvider(
            typeof(Contact), CycleLookup(keyA, keyB, "hierarchy-broken-cycle-a", "hierarchy-broken-cycle-b"))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - the chain resolved because the cycle was broken
        Assert.NotNull(leaf.AccountId);
        Assert.Equal(premadeB.Id, SharedAncestor.GetId("hierarchy-broken-cycle-b"));
    }

    // Batched / deferred main build ----------------------------------

    [Fact]
    public void Supply_WhenDeferred_ResolvesTheSharedAncestorEagerlyAsNow()
    {
        // Arrange - a Deferred build resolves its shared ancestors eagerly, as Now (it cannot defer the
        // pre-phase further than the main graph); this port has no persistence layer, so that eager
        // resolution is what throws - proving the pre-phase, not the main graph, is what got there
        const string name = "hierarchy-deferred";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" });

        // Act
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() => SupplyContactsUnderWithMode(name, 2, InsertMode.Deferred));

        // Assert
        Assert.Contains("persistence gateway", thrown.Message);

        // Cleanup - see SharedAncestorTest.Supply_WhenASharedAncestorIsSelfReferential_ThrowsInsteadOfRecursing
        SharedAncestor.Disable(name);
    }

    // Shaping the shared record's own generation, chained onto Put(...) --------

    [Fact]
    public void Supply_TheSharedRecordTakesValueExpressions()
    {
        // Arrange - a literal and a context-aware expression on the shared Account
        const string name = "hierarchy-shared-value-expressions";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ Ltd" })
            .Put(Field.Of<Account>(x => x.Site), "Berlin")
            .Put(Field.Of<Account>(x => x.Description), new CopyFromSiblingExpression(Field.Of<Account>(x => x.Name)));

        // Act
        Contact leaf = SupplyOneContactUnder(name);

        // Assert
        Account sharedHq = (Account)FirstResolvedAccount(name);
        Assert.Equal("HQ Ltd", sharedHq.Name);
        Assert.Equal("Berlin", sharedHq.Site); // the literal landed on the shared record
        Assert.Equal("HQ Ltd", sharedHq.Description); // the context-aware expression ran on the shared record
        Assert.Equal(sharedHq.Id, leaf.AccountId);
    }

    [Fact]
    public void Supply_TheSharedRecordShapesItsOwnAncestor()
    {
        // Arrange - the shared Account gets a parent Account of its own
        const string name = "hierarchy-shared-shapes-ancestor";
        _ = SharedAncestor.Put(name, new Account { Name = "HQ" })
            .PutRequired(Field.Of<Account>(x => x.ParentId), new DefaultRelationship(new Account { Name = "HQ Parent" }))
            .SetInclusivity(InsertInclusivity.Required);

        // Act
        _ = SupplyOneContactUnder(name);

        // Assert
        Account sharedHq = (Account)FirstResolvedAccount(name);
        Assert.NotNull(sharedHq.ParentId); // the shared record got its configured parent
    }

    // Path-scoped value wires in a shared ancestor -------------------

    [Fact]
    public void Supply_APathValueCanWireInASharedAncestorWithNoSpecialSetup()
    {
        // Arrange - a shared User, reached through Contact.AccountId -> Account.OwnerId
        const string name = "hierarchy-shared-owner-path";
        _ = SharedAncestor.Put(name, new User { LastName = "Owner" });
        List<PropertyInfo> ownerPath = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)];
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
            [LookupKey.Get(typeof(User))] = LeafUserProvider.Instance,
        });

        // Act
        Bundle bundle = new RecordProvider(typeof(Contact), lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .PutRequired(ownerPath, SharedAncestor.Get(name))
            .SupplyBundle();

        // Assert - the shared ancestor was wired in through a path value, nothing declared on the Provider
        Account generatedAccount = (Account)bundle.GetBundle(Field.Of<Contact>(x => x.AccountId))!.PrimaryRecords()![0];
        Assert.Equal(SharedAncestor.GetId(name), generatedAccount.OwnerId);
    }

    // Cross-record-type - one record, two child types -----------------

    [Fact]
    public void Supply_OneSharedRecordServesTwoDifferentChildTypes()
    {
        // Arrange - a Contact Provider and a Case Provider, both under the same shared Account
        const string name = "hierarchy-shared-two-child-types";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = ChildOfSharedProvider.Of<Contact>(nameof(Contact.Id), nameof(Contact.AccountId), nameof(Contact.LastName), name),
            [LookupKey.Get(typeof(Case))] = ChildOfSharedProvider.Of<Case>(nameof(Case.Id), nameof(Case.AccountId), null, name),
        });

        // Act - one supply per record type
        Contact contactRecord = (Contact)new RecordProvider(typeof(Contact), lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();
        Case supportCase = (Case)new RecordProvider(typeof(Case), lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - one HQ for both the Contact and the Case
        Assert.Equal(contactRecord.AccountId, supportCase.AccountId);
        Assert.Equal(SharedAncestor.GetId(name), supportCase.AccountId);
    }

    // End to end - a three-level all-shared spine -------------------

    [Fact]
    public void Supply_AnAllSharedSpineIsBuiltOnceAndEveryLeafLandsOnIt()
    {
        // Arrange - root (flat) <- division (Put-Provider) <- region (Put-Provider)
        const string root = "hierarchy-spine-root";
        const string division = "hierarchy-spine-division";
        const string region = "hierarchy-spine-region";
        ConfigureSpine(root, division, region);

        // Act - two independent leaf batches under the shared region
        List<Contact> eastLeaves = SupplyContactsUnder(region, 3);
        List<Contact> westLeaves = SupplyContactsUnder(region, 2);

        // Assert
        Account rootAccount = (Account)FirstResolvedAccount(root);
        Account divisionAccount = (Account)FirstResolvedAccount(division);
        Account regionAccount = (Account)FirstResolvedAccount(region);
        Assert.Equal(rootAccount.Id, divisionAccount.ParentId); // division -> root
        Assert.Equal(divisionAccount.Id, regionAccount.ParentId); // region -> division
        Assert.All(eastLeaves, leaf => Assert.Equal(regionAccount.Id, leaf.AccountId));
        Assert.All(westLeaves, leaf => Assert.Equal(regionAccount.Id, leaf.AccountId));
        Assert.Equal(rootAccount.Id, SharedAncestor.GetId(root));
        Assert.Equal(divisionAccount.Id, SharedAncestor.GetId(division));
        Assert.Equal(regionAccount.Id, SharedAncestor.GetId(region));
    }

    [Fact]
    public void SupplyBundle_AnAllSharedSpineIsReachableAllTheWayDown()
    {
        // Arrange
        const string root = "hierarchy-spine2-root";
        const string division = "hierarchy-spine2-division";
        const string region = "hierarchy-spine2-region";
        ConfigureSpine(root, division, region);

        // Act
        Bundle leafBundle = new RecordProvider(typeof(Contact), ContactsUnder(region))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyBundle();

        // Assert - the shared record is present at every level of the bundle
        Bundle regionBundle = leafBundle.GetBundle(Field.Of<Contact>(x => x.AccountId))!;
        Bundle divisionBundle = regionBundle.GetBundle(Field.Of<Account>(x => x.ParentId))!;
        Bundle rootBundle = divisionBundle.GetBundle(Field.Of<Account>(x => x.ParentId))!;
        Assert.Equal("Region", ((Account)regionBundle.PrimaryRecords()![0]).Name);
        Assert.Equal("Division", ((Account)divisionBundle.PrimaryRecords()![0]).Name); // present two levels down
        Assert.Equal("Global Root", ((Account)rootBundle.PrimaryRecords()![0]).Name); // and three levels down
    }

    // Fixture - configuration -----------------------------------------------

    private static void ConfigureSpine(string root, string division, string region)
    {
        _ = SharedAncestor.Put(root, new Account { Name = "Global Root" });
        SharedAccountUnder(division, "Division", root);
        SharedAccountUnder(region, "Region", division);
    }

    private static void SharedAccountUnder(string name, string accountName, string sharedParentName) =>
        SharedAncestor.Put(name, new Account { Name = accountName })
            .PutRequired(Field.Of<Account>(x => x.ParentId), SharedAncestor.Get(sharedParentName))
            .SetInclusivity(InsertInclusivity.Required);

    // Fixture - supply helpers -------------------------------------------

    private static List<Contact> SupplyContactsUnder(string sharedName, int howMany) => SupplyContactsUnderWithMode(sharedName, howMany, InsertMode.Mock);

    private static List<Contact> SupplyContactsUnderWithMode(string sharedName, int howMany, InsertMode mode) =>
        [.. new RecordProvider(typeof(Contact), ContactsUnder(sharedName))
            .SetQuantityPerTemplate(howMany)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(mode)
            .SupplyList()
            .Cast<Contact>()];

    private static Contact SupplyOneContactUnder(string sharedName) => SupplyContactsUnder(sharedName, 1)[0];

    private static object FirstResolvedAccount(string sharedName) => SharedAncestor.Get(sharedName).GetResolvedBundle().PrimaryRecords()![0];

    // Fixture - lookups + the Provider double ---------------------------

    private static IProviderLookup ContactsUnder(string sharedName) =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = ChildOfSharedProvider.Of<Contact>(nameof(Contact.Id), nameof(Contact.AccountId), nameof(Contact.LastName), sharedName),
        });

    private static IProviderLookup ContactsUnderWithDefault(string sharedName, Account theDefault) =>
        ProviderLookups.Of(
            new Dictionary<ILookupKey, IRecordProvider>
            {
                [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
                [LookupKey.Get(typeof(Contact))] = ChildOfSharedProvider.Of<Contact>(nameof(Contact.Id), nameof(Contact.AccountId), nameof(Contact.LastName), sharedName),
            },
            new Dictionary<string, object> { [sharedName] = theDefault });

    private static IProviderLookup CycleLookup(ILookupKey keyA, ILookupKey keyB, string nameA, string nameB) =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [keyA] = ChildOfSharedProvider.Of<Account>(nameof(Account.Id), nameof(Account.ParentId), nameof(Account.Name), nameB),
            [keyB] = ChildOfSharedProvider.Of<Account>(nameof(Account.Id), nameof(Account.ParentId), nameof(Account.Name), nameA),
            [LookupKey.Get(typeof(Contact))] = ChildOfSharedProvider.Of<Contact>(nameof(Contact.Id), nameof(Contact.AccountId), nameof(Contact.LastName), nameA),
        });
}

file sealed class LeafUserProvider : IRecordProvider
{
    public static readonly LeafUserProvider Instance = new();

    private static MasterTemplate Template { get; } = new MasterTemplate(Field.Of<User>(x => x.Id))
        .Put(Field.Of<User>(x => x.LastName), new IncrementingStringExpression("User"));

    public PropertyInfo PrimaryTargetField => Field.Of<User>(x => x.Id);

    public MasterTemplate MasterTemplate => Template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, Template, templateRecords);
}

/// <summary>A Provider with one required lookup to a named shared ancestor, plus an optional label field its generation needs.</summary>
file sealed class ChildOfSharedProvider : IRecordProvider
{
    private MasterTemplate _template { get; }

    private ChildOfSharedProvider(PropertyInfo primaryField, MasterTemplate template)
    {
        this.PrimaryTargetField = primaryField;
        this._template = template;
    }

    public static ChildOfSharedProvider Of<TRecord>(string primaryFieldName, string lookupFieldName, string? labelFieldName, string sharedName)
    {
        PropertyInfo primaryField = Field.Of<TRecord>(primaryFieldName);
        MasterTemplate template = new MasterTemplate(primaryField)
            .PutRequired(Field.Of<TRecord>(lookupFieldName), SharedAncestor.Get(sharedName));
        if (labelFieldName is not null)
        {
            _ = template.Put(Field.Of<TRecord>(labelFieldName), new IncrementingStringExpression("X"));
        }

        return new ChildOfSharedProvider(primaryField, template);
    }

    public PropertyInfo PrimaryTargetField { get; }

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
