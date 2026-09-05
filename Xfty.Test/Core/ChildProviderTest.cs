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
/// WithChild(...) and ChildProvider. Mock mode throughout - this port has no
/// persistence layer, so the Apex original's Now/Deferred DML-count
/// assertions are adapted to check the structural bundle shape and the
/// already-established "no persistence layer yet" throw instead (see
/// RecordProviderIntegrationTest).
///
/// Not ported: Apex's two "relationship field does not point at the child/
/// provider type" guard tests - that validation relies on schema describe
/// metadata (SObjectField.getDescribe().getReferenceTo()) with no C#
/// reflection equivalent, and ChildProvider's own doc comment already
/// documents dropping it rather than faking it.
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
            .WithChildren(Field.Of<Contact>(nameof(Contact.AccountId)), 3);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        string accountId = ((Account)bundle.PrimaryRecords()![0]).Id!;
        List<object> contacts = bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId)));
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
            .WithChild(Field.Of<Contact>(nameof(Contact.AccountId)));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        _ = Assert.Single(bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId))));
        Assert.NotNull(bundle.GetChild(Field.Of<Contact>(nameof(Contact.AccountId))));
    }

    [Fact]
    public void Constructor_DerivesTheChildTypeFromTheRelationshipField()
    {
        // Arrange
        // nothing to arrange

        // Act
        ChildProvider childProvider = new(Field.Of<Contact>(nameof(Contact.AccountId)));

        // Assert
        Assert.Equal(typeof(Contact), childProvider.ChildType);
        Assert.Equal(Field.Of<Contact>(nameof(Contact.AccountId)), childProvider.RelationshipField);
    }

    // Templates + multiple configs ----------------------------

    [Fact]
    public void With_AChildProviderTemplate_AppliesToEveryChild()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId)), new Contact { Department = "Buying" }).SetQuantity(2));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<object> children = bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId)));
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
            .With(new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId)), new Contact { Department = "A" }).SetQuantity(3))
            .With(new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId)), new Contact { Department = "B" }).SetQuantity(2));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<Contact> childContacts = [.. bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId))).Cast<Contact>()];
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
            .With(new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId)), new Contact { Department = "A" }).SetQuantity(2))
            .With(new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId)), new Contact { Department = "B" }).SetQuantity(1));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        string parentZero = ((Account)bundle.PrimaryRecords()![0]).Id!;
        string parentOne = ((Account)bundle.PrimaryRecords()![1]).Id!;
        List<Contact> children = [.. bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId))).Cast<Contact>()];
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
            .WithChildren(Field.Of<Contact>(nameof(Contact.AccountId)), 2)
            .WithChildren(Field.Of<Case>(nameof(Case.AccountId)), 3);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Assert.Equal(2, bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId))).Count);
        Assert.Equal(3, bundle.GetChildList(Field.Of<Case>(nameof(Case.AccountId))).Count);
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
            .With(new ChildProvider(Field.Of<Case>(nameof(Case.AccountId))).SetQuantity(2));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        string rootAccountId = ((Account)bundle.PrimaryRecords()![0]).Id!;
        Bundle caseBundle = bundle.GetChildBundle(Field.Of<Case>(nameof(Case.AccountId)))!;
        Assert.Equal(2, caseBundle.PrimaryRecords()!.Count);
        Assert.All(caseBundle.PrimaryRecords()!.Cast<Case>(), caseRecord =>
        {
            Assert.Equal(rootAccountId, caseRecord.AccountId); // root link
            Assert.NotNull(caseRecord.ContactId); // own required Contact parent generated
        });
        List<object> childContacts = caseBundle.GetBundle(Field.Of<Case>(nameof(Case.ContactId)))!.PrimaryRecords()!;
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
                new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId))).SetQuantity(2)
                    .With(new ChildProvider(Field.Of<Case>(nameof(Case.ContactId))).SetQuantity(3)));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<object> contacts = bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId)));
        Assert.Equal(2, contacts.Count);
        List<object> cases = bundle.GetChildBundle(Field.Of<Contact>(nameof(Contact.AccountId)))!.GetChildList(Field.Of<Case>(nameof(Case.ContactId)));
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
                new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId))).SetQuantity(2)
                    .With(new ChildProvider(Field.Of<Case>(nameof(Case.ContactId))).SetQuantity(2)));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - the whole structural graph exists, including grandchildren, before any flush
        List<object> contacts = bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId)));
        Assert.Equal(2, contacts.Count);
        List<object> cases = bundle.GetChildBundle(Field.Of<Contact>(nameof(Contact.AccountId)))!.GetChildList(Field.Of<Case>(nameof(Case.ContactId)));
        Assert.Equal(4, cases.Count);
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(DeferredInserter.Flush);
        Assert.Contains("persistence layer", thrown.Message);
    }

    // Sanity guards -----------------------------------------

    [Fact]
    public void SetQuantity_BelowOne_Throws()
    {
        // Arrange
        ChildProvider childProvider = new(Field.Of<Contact>(nameof(Contact.AccountId)));

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
            .WithChildren(Field.Of<Contact>(nameof(Contact.AccountId)), 2);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - the children got mock Ids too, exactly like the parent (no override given)
        List<object> children = bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId)));
        Assert.Equal(2, children.Count);
        Assert.All(children.Cast<Contact>(), contact => Assert.NotNull(contact.Id));
    }

    [Fact]
    public void With_AChildCanRaiseItsOwnInsertModeAboveTheParents_ButThisPortHasNoRealPersistenceEitherWay()
    {
        // Arrange - parent Never (no persistence attempted), child Now (would be real DML in Apex)
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Never)
            .With(new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId))).SetQuantity(2).SetInsertMode(InsertMode.Now));

        // Act - the child's Now override is honoured (not silently downgraded to the parent's Never), and Now has no
        // persistence layer in this port, so it throws rather than the Apex original's real insert
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(provider.SupplyBundle);

        // Assert
        Assert.Contains("persistence layer", thrown.Message);
    }

    // Apex's mirror-image case (a Now parent with a Mock child) is not portable: with no persistence
    // layer, a Now-mode root always throws NotSupportedException generating its own primary, before
    // SupplyBundle() ever reaches the child-compatibility check at all.
    [Fact]
    public void SupplyBundle_WhenAMockParentHasANowChild_Throws() => AssertMockRealMixThrows(InsertMode.Mock, InsertMode.Now);

    [Fact]
    public void SupplyBundle_WhenTheParentIsLater_LeavesChildrenExactlyAsNeverWould()
    {
        // Arrange - Later is Never-with-intent, all the way down
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Later)
            .WithChildren(Field.Of<Contact>(nameof(Contact.AccountId)), 2);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<Contact> children = [.. bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId))).Cast<Contact>()];
        Assert.Equal(2, children.Count); // the children are still generated
        Assert.Null(children[0].Id); // just not inserted
        Assert.Null(children[0].AccountId); // and the back-reference is null - no parent Id to point at
    }

    [Fact]
    public void SupplyBundle_WhenTheParentIsRelatedOnly_DoesNotPersistItsChildren()
    {
        // Arrange - RelatedOnly does not insert the primaries, so from a child's point of view there is no
        // persisted parent and RelatedOnly flows down
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.RelatedOnly)
            .WithChildren(Field.Of<Contact>(nameof(Contact.AccountId)), 2);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<object> children = bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId)));
        Assert.Equal(2, children.Count);
        Assert.Null(((Contact)children[0]).Id); // children are not inserted
    }

    // Bundle getters ---------------------------------

    [Fact]
    public void GetChildBundle_MergesConfigsAndStaysNavigable()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .With(new ChildProvider(Field.Of<Case>(nameof(Case.AccountId)), new Case { Origin = "Web" }).SetQuantity(1))
            .With(new ChildProvider(Field.Of<Case>(nameof(Case.AccountId)), new Case { Origin = "Phone" }).SetQuantity(1));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Bundle merged = bundle.GetChildBundle(Field.Of<Case>(nameof(Case.AccountId)))!;
        Assert.Equal(2, merged.PrimaryRecords()!.Count);
        Assert.Equal(2, merged.GetBundle(Field.Of<Case>(nameof(Case.ContactId)))!.PrimaryRecords()!.Count); // the Cases from both configs generated their own Contact parent
    }

    // Runners + helpers ----------------------------

    private static void AssertMockRealMixThrows(InsertMode parentMode, InsertMode childMode)
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(parentMode)
            .With(new ChildProvider(Field.Of<Contact>(nameof(Contact.AccountId))).SetInsertMode(childMode));

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
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Case>(nameof(Case.Id)))
        .Put(Field.Of<Case>(nameof(Case.Subject)), new IncrementingStringExpression("Case"))
        .PutRequired(Field.Of<Case>(nameof(Case.ContactId)), new DefaultRelationship(new Contact()));

    public System.Reflection.PropertyInfo PrimaryTargetField => Field.Of<Case>(nameof(Case.Id));

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
