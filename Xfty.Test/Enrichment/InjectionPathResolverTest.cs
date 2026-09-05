using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>
/// Proves InjectionPathResolver - field tokens to a navigation PropertyInfo,
/// against the naming convention this port substitutes for Apex's schema
/// describe (see InjectionPathResolver's own doc comment). No persistence.
///
/// Apex's "no child relationship matches this specific field" case relies on
/// the describe knowing which field backs a named subquery; this port's
/// convention only knows "does the child's own type have a collection
/// property on the parent" - it cannot single out one non-FK field on a type
/// that also has real child fields. The adapted case below uses a parent
/// type with no such collection at all, which both mechanisms reject.
/// </summary>
public class InjectionPathResolverTest
{
    [Fact]
    public void ParentRelationshipField_ForAStandardLookup_ReturnsTheNavigationProperty()
    {
        // Arrange
        PropertyInfo lookup = Field.Of<Contact>(nameof(Contact.AccountId));

        // Act
        PropertyInfo field = InjectionPathResolver.ParentRelationshipField(lookup);

        // Assert
        Assert.Equal(nameof(Contact.Account), field.Name);
    }

    [Fact]
    public void ParentRelationshipField_ForASelfLookup_ReturnsTheSelfNavigationProperty()
    {
        // Arrange
        PropertyInfo selfLookup = Field.Of<Account>(nameof(Account.ParentId));

        // Act
        PropertyInfo field = InjectionPathResolver.ParentRelationshipField(selfLookup);

        // Assert
        Assert.Equal(nameof(Account.Parent), field.Name);
    }

    [Fact]
    public void ParentRelationshipField_ForANonReferenceField_Throws()
    {
        // Arrange
        PropertyInfo notALookup = Field.Of<Account>(nameof(Account.Name));

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => InjectionPathResolver.ParentRelationshipField(notALookup));

        // Assert - the message names the offending field
        Assert.Contains("Name", thrown.Message);
    }

    [Fact]
    public void ChildRelationshipField_ForALookupBackToTheParent_ReturnsTheSubqueryProperty()
    {
        // Arrange
        Type parentType = typeof(Account);

        // Act
        PropertyInfo field = InjectionPathResolver.ChildRelationshipField(parentType, Field.Of<Contact>(nameof(Contact.AccountId)));

        // Assert
        Assert.Equal(nameof(Account.Contacts), field.Name);
    }

    [Fact]
    public void ChildRelationshipField_WhenNoChildRelationshipMatches_Throws()
    {
        // Arrange - Contact has no collection property of its own type
        PropertyInfo notAChildLookup = Field.Of<Contact>(nameof(Contact.Birthdate));

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => InjectionPathResolver.ChildRelationshipField(typeof(Contact), notAChildLookup));

        // Assert
        Assert.NotNull(thrown);
    }
}
