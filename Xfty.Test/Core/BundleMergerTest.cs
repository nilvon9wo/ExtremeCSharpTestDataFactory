using System.Diagnostics.CodeAnalysis;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves BundleMerger - folding the sibling child bundles of one relationship field into a single navigable bundle.
/// Pure in-memory, no database access.
/// </summary>
public class BundleMergerTest
{
    [Fact]
    public void Combine_Always_ConcatenatesEveryBundlesPrimariesInDeclarationOrder()
    {
        // Arrange
        Bundle first = ContactBundle([new Contact { LastName = "A" }]);
        Bundle second = ContactBundle([new Contact { LastName = "B" }, new Contact { LastName = "C" }]);

        // Act
        Bundle merged = BundleMerger.Combine([first, second]);

        // Assert
        List<object> primaries = merged.PrimaryRecords()!;
        Assert.Equal(3, primaries.Count);
        Assert.Equal("A", ((Contact)primaries[0]).LastName);
        Assert.Equal("C", ((Contact)primaries[2]).LastName); // second bundle appended after the first
    }

    [Fact]
    [SuppressMessage("Performance", "HLQ005:Avoid Single() and SingleOrDefault()", Justification = "<Pending>")]
    public void Combine_WhenABundleHasNoPrimaries_SkipsItAndKeepsTheRest()
    {
        // Arrange
        Bundle empty = new();
        empty.PutPrimaries(Field.Of<Contact>(x => x.Id), []);
        Bundle populated = ContactBundle([new Contact { LastName = "Only" }]);

        // Act
        Bundle merged = BundleMerger.Combine([empty, populated]);

        // Assert
        _ = Assert.Single(merged.PrimaryRecords()!);
    }

    [Fact]
    public void Combine_WhenTheFirstBundleHasNoPrimaryTargetField_PutsNoPrimaries()
    {
        // Arrange
        Bundle noField = new();
        Bundle withField = ContactBundle([new Contact { LastName = "Ignored" }]);

        // Act
        Bundle merged = BundleMerger.Combine([noField, withField]);

        // Assert - no primary field on the lead bundle means nothing to key primaries under
        Assert.Null(merged.PrimaryRecords());
    }

    [Fact]
    [SuppressMessage("Performance", "HLQ005:Avoid Single() and SingleOrDefault()", Justification = "<Pending>")]
    public void Combine_WhenAParentFieldIsInOneBundleOnly_CarriesThatSubBundleThrough()
    {
        // Arrange
        Bundle child = ContactBundle([new Contact { LastName = "Child" }]);
        _ = child.Put<Contact>(x => x.AccountId, AccountBundle([new Account { Name = "Parent" }]));
        Bundle plain = ContactBundle([new Contact { LastName = "Sibling" }]);

        // Act
        Bundle merged = BundleMerger.Combine([child, plain]);

        // Assert
        List<object> parents = merged.GetBundle<Contact>(x => x.AccountId)!.PrimaryRecords()!;
        _ = Assert.Single(parents);
        Assert.Equal("Parent", ((Account)parents[0]).Name);
    }

    [Fact]
    public void Combine_WhenAParentFieldIsInBothBundles_CombinesTheirParentPrimaries()
    {
        // Arrange
        Bundle first = ContactBundle([new Contact { LastName = "One" }]);
        _ = first.Put<Contact>(x => x.AccountId, AccountBundle([new Account { Name = "Acme" }]));
        Bundle second = ContactBundle([new Contact { LastName = "Two" }]);
        _ = second.Put<Contact>(x => x.AccountId, AccountBundle([new Account { Name = "Globex" }]));

        // Act
        Bundle merged = BundleMerger.Combine([first, second]);

        // Assert - both bundles contributed their generated parent
        List<object> parents = merged.GetBundle<Contact>(x => x.AccountId)!.PrimaryRecords()!;
        Assert.Equal(2, parents.Count);
    }

    [Fact]
    public void Combine_WhenAParentFieldIsInBothBundles_KeepsTheParentsOwnGeneratedParents()
    {
        // Arrange - each Contact's Account was generated with its own Owner
        Bundle firstAccount = AccountBundle([new Account { Name = "Acme" }]);
        _ = firstAccount.Put<Account>(x => x.OwnerId, UserBundle([new User { LastName = "First Owner" }]));
        Bundle secondAccount = AccountBundle([new Account { Name = "Globex" }]);
        _ = secondAccount.Put<Account>(x => x.OwnerId, UserBundle([new User { LastName = "Second Owner" }]));
        Bundle first = ContactBundle([new Contact { LastName = "One" }]);
        _ = first.Put<Contact>(x => x.AccountId, firstAccount);
        Bundle second = ContactBundle([new Contact { LastName = "Two" }]);
        _ = second.Put<Contact>(x => x.AccountId, secondAccount);

        // Act
        Bundle merged = BundleMerger.Combine([first, second]);

        // Assert
        Bundle owners = merged.GetBundle<Contact>(x => x.AccountId)!.GetBundle<Account>(x => x.OwnerId)!;
        Assert.Equal(2, owners.PrimaryRecords()!.Count);
    }

    private static Bundle ContactBundle(List<object> contacts)
    {
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), contacts);
        return bundle;
    }

    private static Bundle AccountBundle(List<object> accounts)
    {
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Account>(x => x.Id), accounts);
        return bundle;
    }

    private static Bundle UserBundle(List<object> users)
    {
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<User>(x => x.Id), users);
        return bundle;
    }
}