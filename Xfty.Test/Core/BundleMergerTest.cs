using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>Proves BundleMerger - folding the sibling child bundles of one relationship field into a single navigable bundle. Pure in-memory, no DML/SOQL.</summary>
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
    public void Combine_WhenABundleHasNoPrimaries_SkipsItAndKeepsTheRest()
    {
        // Arrange
        Bundle empty = new();
        empty.PutPrimaries(Field.Of<Contact>(nameof(Contact.Id)), []);
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
    public void Combine_WhenAParentFieldIsInOneBundleOnly_CarriesThatSubBundleThrough()
    {
        // Arrange
        Bundle child = ContactBundle([new Contact { LastName = "Child" }]);
        _ = child.Put(Field.Of<Contact>(nameof(Contact.AccountId)), AccountBundle([new Account { Name = "Parent" }]));
        Bundle plain = ContactBundle([new Contact { LastName = "Sibling" }]);

        // Act
        Bundle merged = BundleMerger.Combine([child, plain]);

        // Assert
        List<object> parents = merged.GetBundle(Field.Of<Contact>(nameof(Contact.AccountId)))!.PrimaryRecords()!;
        _ = Assert.Single(parents);
        Assert.Equal("Parent", ((Account)parents[0]).Name);
    }

    [Fact]
    public void Combine_WhenAParentFieldIsInBothBundles_CombinesTheirParentPrimaries()
    {
        // Arrange
        Bundle first = ContactBundle([new Contact { LastName = "One" }]);
        _ = first.Put(Field.Of<Contact>(nameof(Contact.AccountId)), AccountBundle([new Account { Name = "Acme" }]));
        Bundle second = ContactBundle([new Contact { LastName = "Two" }]);
        _ = second.Put(Field.Of<Contact>(nameof(Contact.AccountId)), AccountBundle([new Account { Name = "Globex" }]));

        // Act
        Bundle merged = BundleMerger.Combine([first, second]);

        // Assert - both bundles contributed their generated parent
        List<object> parents = merged.GetBundle(Field.Of<Contact>(nameof(Contact.AccountId)))!.PrimaryRecords()!;
        Assert.Equal(2, parents.Count);
    }

    private static Bundle ContactBundle(List<object> contacts)
    {
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(nameof(Contact.Id)), contacts);
        return bundle;
    }

    private static Bundle AccountBundle(List<object> accounts)
    {
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Account>(nameof(Account.Id)), accounts);
        return bundle;
    }
}
