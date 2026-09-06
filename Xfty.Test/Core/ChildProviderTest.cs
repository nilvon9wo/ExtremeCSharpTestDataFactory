using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>
/// Proves downward generation - RecordProvider.With(...)/WithChildren(...)/
/// WithChild(...) and ChildProvider. Mock mode throughout, checking the
/// structural bundle shape directly; real Now/Deferred insertion is proven
/// separately in PersistenceGatewayTest and RecordProviderIntegrationTest.
///
/// Not covered: a guard rejecting a relationship field that does not point at
/// the child/provider type - validating that would need runtime metadata
/// about what a foreign-key-shaped property conceptually references, which a
/// plain reflection PropertyInfo does not carry (see ChildProvider's own doc
/// comment: a misconfigured field surfaces as a wrong/null value instead of
/// failing fast, and that's a documented, deliberate gap rather than
/// something faked here).
/// </summary>
public class ChildProviderTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
            [LookupKey.Get(typeof(Case))] = new CaseProvider(),
        });

    // Shortcuts -----------------------------------------------------

    [Fact]
    public void WithChildren_GeneratesNChildrenPerPrimaryEachPointingAtItsParent()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 3);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        string accountId = ((Account)bundle.PrimaryRecords()![0]).Id!;
        List<object> contacts = bundle.GetChildList<Contact>(x => x.AccountId);
        Assert.Equal(3, contacts.Count);
        Assert.All(contacts.Cast<Contact>(), contact =>
        {
            Assert.Equal(accountId, contact.AccountId);
            Assert.NotNull(contact.Id);
        });
    }

    [Fact]
    public void WithChild_GeneratesExactlyOneChild()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChild(Field.Of<Contact>(x => x.AccountId));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        _ = Assert.Single(bundle.GetChildList<Contact>(x => x.AccountId));
        Assert.NotNull(bundle.GetChild<Contact>(x => x.AccountId));
    }

    [Fact]
    public void Constructor_DerivesTheChildTypeFromTheRelationshipField()
    {
        // Arrange
        // nothing to arrange

        // Act
        ChildProvider childProvider = new(Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Equal(typeof(Contact), childProvider.ChildType);
        Assert.Equal(Field.Of<Contact>(x => x.AccountId), childProvider.RelationshipField);
    }

    // Templates + multiple configs ----------------------------

    [Fact]
    public void With_AChildProviderTemplate_AppliesToEveryChild()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "Buying" }).SetQuantity(2));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<object> children = bundle.GetChildList<Contact>(x => x.AccountId);
        Assert.Equal(2, children.Count);
        Assert.All(children.Cast<Contact>(), contact => Assert.Equal("Buying", contact.Department));
    }

    [Fact]
    public void With_TwoConfigsOnTheSameField_AreAdditiveAndMultiplyWithTemplateQuantity()
    {
        // Arrange - the child-records.md worked example: 2 templates x SetQuantityPerTemplate(4) = 8 primaries;
        // config A: 3 per primary -> 24; config B: 2 per primary -> 16; total 40.
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetOverrideTemplateList([new Account(), new Account()])
            .SetQuantityPerTemplate(4)
            .SetInsertMode(InsertMode.Mock)
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "A" }).SetQuantity(3))
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "B" }).SetQuantity(2));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<Contact> childContacts = [.. bundle.GetChildList<Contact>(x => x.AccountId).Cast<Contact>()];
        int departmentACount = childContacts.Count(contact => contact.Department == "A");
        Assert.Equal(8, bundle.PrimaryRecords()!.Count);
        Assert.Equal(40, childContacts.Count);
        Assert.Equal(24, departmentACount);
        Assert.Equal(16, childContacts.Count - departmentACount);
    }

    [Fact]
    public void GetChildList_OrderIsConfigThenPrimaryThenQuantity()
    {
        // Arrange - 2 primaries, config A (q2) then config B (q1):
        // A/P0, A/P0, A/P1, A/P1, B/P0, B/P1
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetQuantityPerTemplate(2)
            .SetInsertMode(InsertMode.Mock)
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "A" }).SetQuantity(2))
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "B" }).SetQuantity(1));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        string parentZero = ((Account)bundle.PrimaryRecords()![0]).Id!;
        string parentOne = ((Account)bundle.PrimaryRecords()![1]).Id!;
        List<Contact> children = [.. bundle.GetChildList<Contact>(x => x.AccountId).Cast<Contact>()];
        Assert.Equal(6, children.Count);
        AssertContact(children[0], "A", parentZero);
        AssertContact(children[1], "A", parentZero);
        AssertContact(children[2], "A", parentOne);
        AssertContact(children[3], "A", parentOne);
        AssertContact(children[4], "B", parentZero);
        AssertContact(children[5], "B", parentOne);
    }

    // Several child types ------------------------------------

    [Fact]
    public void WithChildren_ForSeveralChildTypes_AppliesThemConcurrently()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 2)
            .WithChildren(Field.Of<Case>(x => x.AccountId), 3);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Assert.Equal(2, bundle.GetChildList<Contact>(x => x.AccountId).Count);
        Assert.Equal(3, bundle.GetChildList<Case>(x => x.AccountId).Count);
        Assert.Equal(2, bundle.ChildRelationshipFields().Count);
    }

    // Children's own parents --------------------------------

    [Fact]
    public void With_AtRequiredInclusivity_EachChildGeneratesItsOwnRequiredParent()
    {
        // Arrange - Account -> child Case (Case.AccountId) -> Case's own required Contact -> that Contact's Account
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .With(ChildProvider.For<Case>(x => x.AccountId).SetQuantity(2));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        string rootAccountId = ((Account)bundle.PrimaryRecords()![0]).Id!;
        Bundle caseBundle = bundle.GetChildBundle<Case>(x => x.AccountId)!;
        Assert.Equal(2, caseBundle.PrimaryRecords()!.Count);
        Assert.All(caseBundle.PrimaryRecords()!.Cast<Case>(), caseRecord =>
        {
            Assert.Equal(rootAccountId, caseRecord.AccountId); // root link
            Assert.NotNull(caseRecord.ContactId); // own required Contact parent generated
        });
        List<object> childContacts = caseBundle.GetBundle<Case>(x => x.ContactId)!.PrimaryRecords()!;
        Assert.Equal(2, childContacts.Count);
        Assert.All(childContacts.Cast<Contact>(), contact =>
        {
            Assert.NotNull(contact.AccountId);
            Assert.NotEqual(rootAccountId, contact.AccountId); // a distinct Account, not the root
        });
    }

    // Grandchildren -----------------------------------------

    [Fact]
    public void With_NestedChildProviders_GenerateGrandchildrenPointingAtTheirParent()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(
                ChildProvider.For<Contact>(x => x.AccountId).SetQuantity(2)
                    .With(ChildProvider.For<Case>(x => x.ContactId).SetQuantity(3)));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<object> contacts = bundle.GetChildList<Contact>(x => x.AccountId);
        Assert.Equal(2, contacts.Count);
        List<object> cases = bundle.GetChildBundle<Contact>(x => x.AccountId)!.GetChildList<Case>(x => x.ContactId);
        Assert.Equal(6, cases.Count); // 2 Contacts x 3 Cases
        HashSet<string?> contactIds = [.. contacts.Cast<Contact>().Select(contact => contact.Id)];
        Assert.All(cases.Cast<Case>(), caseRecord => Assert.Contains(caseRecord.ContactId, contactIds)); // grandchild points at its Contact
    }

    [Fact]
    public void SupplyBundle_InDeferredMode_BuildsEveryLevelIncludingGrandchildrenStructurally()
    {
        // Arrange - DEFERRED builds the whole graph structurally; Flush() is where real persistence would happen
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Deferred)
            .With(
                ChildProvider.For<Contact>(x => x.AccountId).SetQuantity(2)
                    .With(ChildProvider.For<Case>(x => x.ContactId).SetQuantity(2)));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - the whole structural graph exists, including grandchildren, before any flush
        List<object> contacts = bundle.GetChildList<Contact>(x => x.AccountId);
        Assert.Equal(2, contacts.Count);
        List<object> cases = bundle.GetChildBundle<Contact>(x => x.AccountId)!.GetChildList<Case>(x => x.ContactId);
        Assert.Equal(4, cases.Count);
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() => DeferredInserter.Flush());
        Assert.Contains("persistence gateway", thrown.Message);
        DeferredInserter.ResetForTesting(); // the failed Flush() deliberately left the registry non-empty
    }

    // Sanity guards -----------------------------------------

    [Fact]
    public void SetQuantity_BelowOne_Throws()
    {
        // Arrange
        ChildProvider childProvider = new(Field.Of<Contact>(x => x.AccountId));

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => childProvider.SetQuantity(0));

        // Assert
        Assert.Contains("at least 1", thrown.Message);
    }

    // Insert mode --------------------------------------

    [Fact]
    public void WithChildren_ByDefault_TheChildInheritsTheParentInsertMode()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 2);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - the children got mock Ids too, exactly like the parent (no override given)
        List<object> children = bundle.GetChildList<Contact>(x => x.AccountId);
        Assert.Equal(2, children.Count);
        Assert.All(children.Cast<Contact>(), contact => Assert.NotNull(contact.Id));
    }

    [Fact]
    public void With_AChildCanRaiseItsOwnInsertModeAboveTheParents_ButNowWithoutAGatewayStillThrows()
    {
        // Arrange - parent Never (no persistence attempted), child Now with no gateway configured
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Never)
            .With(ChildProvider.For<Contact>(x => x.AccountId).SetQuantity(2).SetInsertMode(InsertMode.Now));

        // Act - the child's Now override is honoured (not silently downgraded to the parent's Never); Now with no
        // gateway configured throws rather than silently skipping the insert (see PersistenceGatewayTest for the
        // configured-gateway case, where Now genuinely persists)
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(provider.SupplyBundle);

        // Assert
        Assert.Contains("persistence gateway", thrown.Message);
    }

    // Mock and Now are rejected as a mix in either direction - a mock Id and a real inserted row can
    // never coexist correctly in the same generated graph, whether or not a persistence gateway is
    // configured for Now.
    [Fact]
    public void SupplyBundle_WhenAMockParentHasANowChild_Throws() => AssertMockRealMixThrows(InsertMode.Mock, InsertMode.Now);

    [Fact]
    public void SupplyBundle_WhenTheParentIsLater_LeavesChildrenExactlyAsNeverWould()
    {
        // Arrange - Later is Never-with-intent, all the way down
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Later)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 2);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<Contact> children = [.. bundle.GetChildList<Contact>(x => x.AccountId).Cast<Contact>()];
        Assert.Equal(2, children.Count); // the children are still generated
        Assert.Null(children[0].Id); // just not inserted
        Assert.Null(children[0].AccountId); // and the back-reference is null - no parent Id to point at
    }

    [Fact]
    public void SupplyBundle_WhenTheParentExcludesPrimaryIds_ChildrenStillGetTheirOwnIdButNoBackReference()
    {
        // Arrange - ExcludePrimaryIds only ever excludes the call's own primary; a child inherits the
        // parent's InsertMode (Mock), not its exclusion, so it still gets its own mock Id - just with
        // nothing to point its FK at, since the parent it would reference was never given one
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .ExcludePrimaryIds()
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 2);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<object> children = bundle.GetChildList<Contact>(x => x.AccountId);
        Assert.Equal(2, children.Count);
        Assert.NotNull(((Contact)children[0]).Id); // the child's own Id is unaffected
        Assert.Null(((Contact)children[0]).AccountId); // no parent Id to point at
    }

    // Bundle getters ---------------------------------

    [Fact]
    public void GetChildBundle_MergesConfigsAndStaysNavigable()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .With(ChildProvider.For<Case>(x => x.AccountId, new Case { Origin = "Web" }).SetQuantity(1))
            .With(ChildProvider.For<Case>(x => x.AccountId, new Case { Origin = "Phone" }).SetQuantity(1));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Bundle merged = bundle.GetChildBundle<Case>(x => x.AccountId)!;
        Assert.Equal(2, merged.PrimaryRecords()!.Count);
        Assert.Equal(2, merged.GetBundle<Case>(x => x.ContactId)!.PrimaryRecords()!.Count); // the Cases from both configs generated their own Contact parent
    }

    // Runners + helpers ----------------------------

    private static void AssertMockRealMixThrows(InsertMode parentMode, InsertMode childMode)
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(parentMode)
            .With(ChildProvider.For<Contact>(x => x.AccountId).SetInsertMode(childMode));

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.SupplyBundle);

        // Assert - a mock/real mix must throw
        Assert.Contains("mix mock", thrown.Message);
    }

    private static void AssertContact(Contact childContact, string department, string accountId)
    {
        Assert.Equal(department, childContact.Department);
        Assert.Equal(accountId, childContact.AccountId);
    }
}

/// <summary>Case that needs a Contact (which in turn needs its own Account) - an in-test Provider only used here.</summary>
file sealed class CaseProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Case>(x => x.Id))
        .Put<Case>(x => x.Subject, new IncrementingStringExpression("Case"))
        .PutRequired<Case>(x => x.ContactId, new DefaultRelationship(new Contact()));

    public System.Reflection.PropertyInfo PrimaryTargetField => Field.Of<Case>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
