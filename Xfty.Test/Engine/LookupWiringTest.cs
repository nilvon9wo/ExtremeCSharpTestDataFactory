using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Test.Engine;

/// <summary>
/// Proves <see cref="LookupWiring"/> directly - the end-to-end wiring is
/// proven throughout the RecordProvider tests; this covers the shapes a
/// generated bundle never takes.
/// </summary>
public class LookupWiringTest
{
    [Fact]
    public void Wire_WhenTheParentHasNoKeyedSubBundle_LeavesTheLookupUnset()
    {
        // Arrange - the parent row is there, but no sub-bundle says which of its fields is the key
        Contact contact = new();
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [contact]);
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account { Id = "A-1" }]);
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account()));

        // Act
        new LookupWiring(bundle, template).Wire();

        // Assert
        Assert.Null(contact.AccountId);
    }
}