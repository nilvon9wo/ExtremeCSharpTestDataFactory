using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>Proves EnrichmentTarget - resolving the Inject(field, ...) field to the records, sub-bundle and generated-ancestor flag. Pure in-memory.</summary>
public class EnrichmentTargetTest
{
    [Fact]
    public void Locate_ForThePrimaryField_ReturnsThePrimariesAndTheBundleItself()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact()]);

        // Act
        EnrichmentTarget target = EnrichmentTarget.Locate(bundle, Field.Of<Contact>(x => x.Id));

        // Assert
        _ = Assert.Single(target.Records!);
        Assert.False(target.IsGeneratedAncestor);
    }

    [Fact]
    public void Locate_ForAGeneratedAncestorField_FlagsItAsAnAncestor()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact()]);
        _ = bundle.Put(Field.Of<Contact>(x => x.AccountId), [new Account()]);
        _ = bundle.Put(Field.Of<Contact>(x => x.AccountId), new Bundle());

        // Act
        EnrichmentTarget target = EnrichmentTarget.Locate(bundle, Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.True(target.IsGeneratedAncestor);
    }

    [Fact]
    public void Locate_ForAChildField_ReturnsTheChildList()
    {
        // Arrange
        Bundle childBundle = new();
        childBundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact(), new Contact()]);
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account()]);
        _ = bundle.PutChild(Field.Of<Contact>(x => x.AccountId), childBundle, [0, 0]);

        // Act
        EnrichmentTarget target = EnrichmentTarget.Locate(bundle, Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Equal(2, target.Records!.Count);
        Assert.False(target.IsGeneratedAncestor);
    }

    [Fact]
    public void Locate_ForAnUnknownField_Throws()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact()]);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => EnrichmentTarget.Locate(bundle, Field.Of<Account>(x => x.Name)));

        // Assert
        Assert.NotNull(thrown);
    }

    [Fact]
    public void HasAnythingToInject_WhenThereAreNoAncestorsNorChildren_IsFalse()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact()]);

        // Act
        bool anything = EnrichmentTarget.Locate(bundle, Field.Of<Contact>(x => x.Id)).HasAnythingToInject();

        // Assert
        Assert.False(anything);
    }

    [Fact]
    public void HasAnythingToInject_WhenThereIsAGeneratedAncestor_IsTrue()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact()]);
        _ = bundle.Put(Field.Of<Contact>(x => x.AccountId), new Bundle());

        // Act
        bool anything = EnrichmentTarget.Locate(bundle, Field.Of<Contact>(x => x.Id)).HasAnythingToInject();

        // Assert
        Assert.True(anything);
    }
}
