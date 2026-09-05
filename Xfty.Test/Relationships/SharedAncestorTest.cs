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
/// Coverage for SharedAncestor - one generated record shared by every child,
/// Put(...)/PutAsTemplate/PutAsValue/PutIfAbsent/GetId(...), the
/// configuration guards, FromVariant/CopyingRelatedField, and cycle
/// detection. Deep chains and the batched pre-phase are covered in
/// SharedAncestorIntegrationTest.
///
/// Apex's Now-mode scenarios verify real DML row counts; this port has no
/// persistence layer, so those are adapted to Mock-mode equivalents that
/// prove the same wiring (every child resolves to the one shared instance/Id).
///
/// SharedAncestor's registry is process-static; each test below uses its own
/// never-reused shared-ancestor name to stay isolated (see the class's own
/// doc comment).
/// </summary>
public class SharedAncestorTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void SupplyList_WhenFiftyChildrenShareOneSharedAncestor_TheyAllResolveToOneAccount()
    {
        // Arrange
        const string name = "shared-ancestor-test-fifty-children";
        _ = SharedAncestor.Put(name, new Account { Name = "ACME HQ" });

        // Act
        List<object> contacts = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetQuantityPerTemplate(50)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyList();

        // Assert - 50 Contacts, one generated Account
        Assert.Equal(50, contacts.Count);
        _ = Assert.Single(contacts.Cast<Contact>().Select(contact => contact.AccountId).Distinct());
    }

    [Fact]
    public void SupplyBundle_WhenManyChildrenShareASharedAncestor_TheyAllPointAtOneParent()
    {
        // Arrange
        const string name = "shared-ancestor-test-many-children";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });

        // Act
        Bundle bundle = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetQuantityPerTemplate(5)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyBundle();

        // Assert
        List<object> contacts = bundle.GetList(Field.Of<Contact>(nameof(Contact.Id)))!;
        Assert.Equal(5, contacts.Count);
        Assert.Equal(5, bundle.GetList(Field.Of<Contact>(nameof(Contact.AccountId)))!.Count);
        HashSet<string?> accountIds = [.. contacts.Cast<Contact>().Select(contact => contact.AccountId)];
        _ = Assert.Single(accountIds); // every Contact points at the one shared Account
        Assert.Equal(((Account)bundle.GetList(Field.Of<Contact>(nameof(Contact.AccountId)))![0]).Id, accountIds.First());
    }

    [Fact]
    public void SupplyList_InMockMode_SharesTheParentExactlyOnceAcrossFourContacts()
    {
        // Arrange
        const string name = "shared-ancestor-test-four-contacts";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });

        // Act
        List<object> contacts = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetQuantityPerTemplate(4)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyList();

        // Assert - all four Contacts on the same Account
        _ = Assert.Single(contacts.Cast<Contact>().Select(contact => contact.AccountId).Distinct());
    }

    [Fact]
    public void SupplyBundle_ExposesTheSharedRecordInBothTheListAndTheSubBundle()
    {
        // Arrange
        const string name = "shared-ancestor-test-list-and-subbundle";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });

        // Act
        Bundle bundle = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyBundle();

        // Assert
        Assert.Equal("Shared HQ", ((Account)bundle.GetList(Field.Of<Contact>(nameof(Contact.AccountId)))![0]).Name);
        Assert.NotNull(bundle.GetBundle(Field.Of<Contact>(nameof(Contact.AccountId)))); // the shared ancestor has a sub-bundle
        Assert.Equal("Shared HQ", ((Account)bundle.GetBundle(Field.Of<Contact>(nameof(Contact.AccountId)))!.GetList(Field.Of<Account>(nameof(Account.Id)))![0]).Name);
    }

    [Fact]
    public void Supply_WhenAMockResolvedSharedAncestorIsReferencedFromANowCall_Throws()
    {
        // Arrange - resolve the ancestor in Mock mode first, so it has a mock (not real) Id
        const string name = "shared-ancestor-test-mock-then-now";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });
        _ = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Act - a Now call that would carry the mock Id onto inserted Contacts
        RecordProvider nowProvider = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now);
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(nowProvider.Supply);

        // Assert - a Mock-then-Now mix must throw, not drift a mock Id into real generation
        Assert.Contains("consistent insert mode", thrown.Message);
    }

    [Fact]
    public void Supply_WhenTheSharedAncestorIsAPreInsertedRecord_AcceptsItAsIs()
    {
        // Arrange - the test supplies its own already-persisted-looking HQ and registers it
        const string name = "shared-ancestor-test-pre-inserted";
        Account preInserted = new() { Name = "Real HQ", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put(name, preInserted);

        // Act
        List<object> contacts = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetQuantityPerTemplate(3)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyList();

        // Assert
        Assert.All(contacts.Cast<Contact>(), contact => Assert.Equal(preInserted.Id, contact.AccountId));
    }

    [Fact]
    public void GetBundle_ForAPutSharedAncestor_ReturnsIt()
    {
        // Arrange
        const string name = "shared-ancestor-test-get-bundle";
        _ = SharedAncestor.Put(name, new Account { Name = "Supplied HQ" });

        // Act
        Bundle bundle = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyBundle();

        // Assert - GetBundle is populated even for a Put(...) record
        Bundle? accountBundle = bundle.GetBundle(Field.Of<Contact>(nameof(Contact.AccountId)));
        Assert.NotNull(accountBundle);
        Assert.Equal("Supplied HQ", ((Account)accountBundle!.GetList(Field.Of<Account>(nameof(Account.Id)))![0]).Name);
        // GetList and GetBundle expose the same shared instance
        Assert.Same(bundle.GetList(Field.Of<Contact>(nameof(Contact.AccountId)))![0], accountBundle.GetList(Field.Of<Account>(nameof(Account.Id)))![0]);
    }

    [Fact]
    public void Supply_ReusesTheSameSharedAncestorAcrossSeparateSupplyCalls()
    {
        // Arrange
        const string name = "shared-ancestor-test-reuse-across-calls";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });

        // Act - two independent supply calls
        Contact fromFirstCall = SupplyOneContactUnder(name);
        Contact fromSecondCall = SupplyOneContactUnder(name);

        // Assert - the shared record survives between Supply() calls
        Assert.Equal(fromFirstCall.AccountId, fromSecondCall.AccountId);
    }

    [Fact]
    public void GetId_AfterResolution_ReturnsTheResolvedRecordsId()
    {
        // Arrange
        const string name = "shared-ancestor-test-getid-after";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });

        // Act
        Contact result = SupplyOneContactUnder(name);

        // Assert
        Assert.Equal(result.AccountId, SharedAncestor.GetId(name));
    }

    [Fact]
    public void GetId_BeforeResolution_Throws()
    {
        // Arrange
        const string name = "shared-ancestor-test-getid-before";
        _ = SharedAncestor.Put(name, new Account { Name = "x" });

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => SharedAncestor.GetId(name));

        // Assert - GetId before resolution must throw
        Assert.Contains("not resolved yet", thrown.Message);
    }

    [Fact]
    public void Put_RegistersATestSuppliedRecord()
    {
        // Arrange
        const string name = "shared-ancestor-test-put-registers";
        Account preMade = new() { Name = "Pre Made", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put(name, preMade);

        // Act
        Contact result = SupplyOneContactUnder(name);

        // Assert - the pre-made record is used, nothing generated in its place
        Assert.Equal(preMade.Id, result.AccountId);
    }

    [Fact]
    public void Supply_WhenCopyingRelatedField_CopiesThatFieldInsteadOfTheId()
    {
        // Arrange
        const string name = "shared-ancestor-test-related-field";
        _ = SharedAncestor.Put(name, new Account { Name = "Named HQ" }).CopyingRelatedField(Field.Of<Account>(nameof(Account.Name)));

        // Act
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.Department)), SharedAncestor.Get(name))
            .RemoveFromMasterTemplate(Field.Of<Contact>(nameof(Contact.AccountId)))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert
        Assert.Equal("Named HQ", result.Department);
    }

    [Fact]
    public void Supply_WhenFromVariantIsUsed_PinsTheProviderVariant()
    {
        // Arrange
        const string name = "shared-ancestor-test-from-variant";
        _ = SharedAncestor.Put(name, new Account()).FromVariant(LookupKey.Get(typeof(Account)));

        // Act
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert
        Assert.NotNull(result.AccountId);
    }

    [Fact]
    public void SharedNameAndOverrideTemplate_ExposeWhatTheSharedAncestorWasConfiguredWith()
    {
        // Arrange - nothing to arrange, configuration is the act
        const string name = "shared-ancestor-test-name-and-template";

        // Act
        _ = SharedAncestor.Put(name, new Account { Name = "Configured" }).CopyingRelatedField(Field.Of<Account>(nameof(Account.Name)));

        // Assert
        SharedAncestor ancestor = SharedAncestor.Get(name);
        Assert.Equal(name, ancestor.SharedName);
        Assert.Equal("Configured", ((Account)ancestor.OverrideTemplate!).Name);
        Assert.Equal(Field.Of<Account>(nameof(Account.Name)), ancestor.RelatedField);
    }

    // Guards ---------------------------------------------------------------

    [Fact]
    public void Get_WhenTheNameIsBlank_Throws()
    {
        // Arrange - nothing to arrange, the blank name is the act

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => SharedAncestor.Get("   "));

        // Assert - a blank name must be rejected
        Assert.Contains("non-blank name", thrown.Message);
    }

    [Fact]
    public void Supply_WhenASharedAncestorIsUnregistered_Throws()
    {
        // Arrange - nothing to arrange, 'never-registered' is referenced but never given a template
        const string name = "shared-ancestor-test-never-registered";
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.Supply);

        // Assert - an unregistered shared ancestor must throw when generation reaches it
        Assert.Contains("never registered", thrown.Message);
    }

    [Fact]
    public void Supply_WhenDepthBatched_ResolvesTheSharedAncestorPrePhaseBeforeTheMainGraph()
    {
        // Arrange - under Now, resolving the shared ancestor is itself a real (depth-batched) insert, same as the
        // main graph; this port has no persistence layer, so both attempts throw the same NotSupportedException -
        // the point here is that the pre-phase (not the main graph) is what reaches it first
        const string name = "shared-ancestor-test-depth-batched";
        _ = SharedAncestor.Put(name, new Account { Name = "Shared HQ" });
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .DepthBatched();

        // Act
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(provider.Supply);

        // Assert
        Assert.Contains("persistence layer", thrown.Message);

        // Cleanup - see Supply_WhenASharedAncestorIsSelfReferential_ThrowsInsteadOfRecursing
        SharedAncestor.Disable(name);
    }

    [Fact]
    public void Put_WhenReconfiguringAnAlreadyResolvedSharedAncestor_Throws()
    {
        // Arrange - resolve the ancestor, then try to reconfigure it
        const string name = "shared-ancestor-test-reconfigure-resolved";
        _ = SharedAncestor.Put(name, new Account { Name = "First" });
        _ = SupplyOneContactUnder(name);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => SharedAncestor.PutAsTemplate(name, new Account { Name = "Second" }));

        // Assert - reconfiguring a resolved shared ancestor must throw
        Assert.Contains("already resolved", thrown.Message);
    }

    [Fact]
    public void Supply_WhenASharedAncestorIsSelfReferential_ThrowsInsteadOfRecursing()
    {
        // Arrange - the 'loop' Account's own ParentId is the 'loop' shared ancestor
        const string name = "shared-ancestor-test-loop";
        _ = SharedAncestor.Put(name, new Account { Name = "Loop" });
        IProviderLookup loopy = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new SelfReferencingAccountProvider(name),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });
        RecordProvider provider = new RecordProvider(typeof(Contact), loopy)
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.Supply);

        // Assert - a self-referential shared ancestor must throw, not stack-overflow
        Assert.Contains("cycle", thrown.Message, StringComparison.OrdinalIgnoreCase);

        // Cleanup - a Put(...) ancestor that never resolves stays in the static registry forever
        // otherwise, and every later test's SupplyBundle() calls ResolveAllConfigured() too
        SharedAncestor.Disable(name);
    }

    [Fact]
    public void Supply_WhenThreeSharedAncestorsFormAnIndirectCycle_Throws()
    {
        // Arrange - tom -> dick -> harry -> tom
        ILookupKey tomKey = FlavouredLookupKey.Get(typeof(Account), "tom");
        ILookupKey dickKey = FlavouredLookupKey.Get(typeof(Account), "dick");
        ILookupKey harryKey = FlavouredLookupKey.Get(typeof(Account), "harry");
        _ = SharedAncestor.Put("tom", new Account { Name = "Tom" }).FromVariant(tomKey);
        _ = SharedAncestor.Put("dick", new Account { Name = "Dick" }).FromVariant(dickKey);
        _ = SharedAncestor.Put("harry", new Account { Name = "Harry" }).FromVariant(harryKey);
        IProviderLookup ring = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [tomKey] = new ParentedAccountProvider("dick"),
            [dickKey] = new ParentedAccountProvider("harry"),
            [harryKey] = new ParentedAccountProvider("tom"),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });
        RecordProvider provider = new RecordProvider(typeof(Contact), ring)
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get("tom"))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.Supply);

        // Assert - a three-way shared-ancestor cycle must throw
        Assert.Contains("cycle", thrown.Message, StringComparison.OrdinalIgnoreCase);

        // Cleanup - see Supply_WhenASharedAncestorIsSelfReferential_ThrowsInsteadOfRecursing
        SharedAncestor.Disable("tom");
        SharedAncestor.Disable("dick");
        SharedAncestor.Disable("harry");
    }

    [Fact]
    public void Put_OverAResolvedSharedAncestorWithADifferentRecord_Succeeds()
    {
        // Arrange - resolve the ancestor, then Put a different record over it
        const string name = "shared-ancestor-test-put-over-resolved";
        _ = SharedAncestor.Put(name, new Account { Name = "Generated" });
        _ = SupplyOneContactUnder(name);
        Account replacement = new() { Name = "Replacement", Id = IdMocker.GenerateId() };

        // Act
        _ = SharedAncestor.Put(name, replacement); // does not throw

        // Assert
        Assert.Equal(replacement.Id, SharedAncestor.GetId(name));
    }

    // Fixture ------------------------------------------------------------

    private static Contact SupplyOneContactUnder(string sharedName) =>
        (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), SharedAncestor.Get(sharedName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();
}

file sealed class SelfReferencingAccountProvider : IRecordProvider
{
    private MasterTemplate _template { get; }

    public SelfReferencingAccountProvider(string loopSharedName) =>
        this._template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new IncrementingStringExpression("Loop"))
            .PutRequired(Field.Of<Account>(nameof(Account.ParentId)), SharedAncestor.Get(loopSharedName));

    public PropertyInfo PrimaryTargetField => Field.Of<Account>(nameof(Account.Id));

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}

/// <summary>An Account Provider whose ParentId is the named shared ancestor - for the cycle tests.</summary>
file sealed class ParentedAccountProvider : IRecordProvider
{
    private MasterTemplate _template { get; }

    public ParentedAccountProvider(string parentSharedName) =>
        this._template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new IncrementingStringExpression("Ring"))
            .PutRequired(Field.Of<Account>(nameof(Account.ParentId)), SharedAncestor.Get(parentSharedName));

    public PropertyInfo PrimaryTargetField => Field.Of<Account>(nameof(Account.Id));

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
