using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Test.Persistence;

/// <summary>
/// Proves DeferredInsertBuffer driven directly with hand-built bundles,
/// including the guards the engine never trips, via Add(bundle) +
/// ResolveAll(Mock) - the same collect/link/layer algorithm a real Now
/// insert runs, provable here without a database. InsertGraph(...) against a
/// real persistence gateway is proven in PersistenceGatewayTest.
/// </summary>
public class DeferredInsertBufferTest
{
    [Fact]
    public async Task ResolveAll_ForANullBundle_DoesNothing()
    {
        // Arrange
        DeferredInsertBuffer buffer = new();
        buffer.Add(null);

        // Act / Assert - no throw
        await buffer.ResolveAll(InsertMode.Mock);
    }

    [Fact]
    public async Task ResolveAll_ForABundleWithNoPrimaryRecords_DoesNothing()
    {
        // Arrange
        DeferredInsertBuffer buffer = new();
        buffer.Add(new Bundle());

        // Act / Assert - no throw
        await buffer.ResolveAll(InsertMode.Mock);
    }

    [Fact]
    public async Task ResolveAll_ForAParentAndChild_ResolvesTheParentFirstAndPointsTheLookup()
    {
        // Arrange
        Account parent = new() { Name = "Buffer Parent" };
        Contact child = new() { LastName = "Buffer Child" };
        Bundle childBundle = BundleOf(Field.Of<Contact>(x => x.Id), child);
        _ = childBundle.Put<Contact>(x => x.AccountId, BundleOf(Field.Of<Account>(x => x.Id), parent));
        _ = childBundle.Put<Contact>(x => x.AccountId, [parent]);
        DeferredInsertBuffer buffer = new();
        buffer.Add(childBundle);

        // Act
        await buffer.ResolveAll(InsertMode.Mock);

        // Assert - the child lookup points at the freshly-mocked parent Id
        Assert.Equal(parent.Id, child.AccountId);
    }

    [Fact]
    public async Task ResolveAll_WhenAChildLookupIsAlreadySet_LeavesItAlone()
    {
        // Arrange
        Account existing = new() { Name = "Already Linked", Id = IdMocker.GenerateId() };
        Account generatedParent = new() { Name = "Generated Parent" };
        Contact child = new() { LastName = "Pre Linked", AccountId = existing.Id };
        Bundle childBundle = BundleOf(Field.Of<Contact>(x => x.Id), child);
        _ = childBundle.Put<Contact>(x => x.AccountId, BundleOf(Field.Of<Account>(x => x.Id), generatedParent));
        _ = childBundle.Put<Contact>(x => x.AccountId, [generatedParent]);
        DeferredInsertBuffer buffer = new();
        buffer.Add(childBundle);

        // Act
        await buffer.ResolveAll(InsertMode.Mock);

        // Assert
        Assert.Equal(existing.Id, child.AccountId); // the pre-set lookup is not repointed
        Assert.NotNull(generatedParent.Id); // the generated parent still resolves
    }

    [Fact]
    public async Task ResolveAll_WhenARelationshipFieldHasNoSubBundle_SkipsItAndStillResolvesThePrimary()
    {
        // Arrange
        Account onlyRecord = new() { Name = "No Parent Bundle" };
        Bundle bundle = BundleOf(Field.Of<Account>(x => x.Id), onlyRecord);
        _ = bundle.Put<Account>(x => x.ParentId, (Bundle)null!);
        DeferredInsertBuffer buffer = new();
        buffer.Add(bundle);

        // Act
        await buffer.ResolveAll(InsertMode.Mock);

        // Assert
        Assert.NotNull(onlyRecord.Id);
    }

    [Fact]
    public async Task ResolveAll_ForOneParentSharedByManyChildren_PointsEveryChildAtIt()
    {
        // Arrange - a shared ancestor: 3 Contacts, one Account bundle
        Account sharedParent = new() { Name = "Shared Parent" };
        List<object> contacts = [new Contact { LastName = "A" }, new Contact { LastName = "B" }, new Contact { LastName = "C" }];
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), contacts);
        _ = bundle.Put<Contact>(x => x.AccountId, BundleOf(Field.Of<Account>(x => x.Id), sharedParent));
        _ = bundle.Put<Contact>(x => x.AccountId, [sharedParent]);
        DeferredInsertBuffer buffer = new();
        buffer.Add(bundle);

        // Act
        await buffer.ResolveAll(InsertMode.Mock);

        // Assert
        Assert.All(contacts.Cast<Contact>(), contact => Assert.Equal(sharedParent.Id, contact.AccountId));
    }

    [Fact]
    public void Flatten_ReturnsEveryRecordAndItsParentLink_WithoutResolvingAnything()
    {
        // Arrange
        Account parent = new() { Name = "Flatten Parent" };
        Contact child = new() { LastName = "Flatten Child" };
        Bundle childBundle = BundleOf(Field.Of<Contact>(x => x.Id), child);
        _ = childBundle.Put<Contact>(x => x.AccountId, BundleOf(Field.Of<Account>(x => x.Id), parent));
        _ = childBundle.Put<Contact>(x => x.AccountId, [parent]);

        // Act
        DeferredInsertBuffer graph = DeferredInsertBuffer.Flatten(childBundle);

        // Assert
        Assert.Equal(2, graph.Records().Count); // the child and its parent
        _ = Assert.Single(graph.ParentLinks()); // one lookup to wire
        Assert.Null(child.Id); // nothing was resolved
    }

    // Helpers -----------------------------------------------

    private static Bundle BundleOf(System.Reflection.PropertyInfo primaryField, object record)
    {
        Bundle bundle = new();
        bundle.PutPrimaries(primaryField, [record]);
        return bundle;
    }
}
