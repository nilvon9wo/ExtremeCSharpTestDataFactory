using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>
/// Proves InjectionPathResolver - field tokens to a navigation PropertyInfo,
/// resolved by naming convention rather than schema metadata (see
/// InjectionPathResolver's own doc comment). No persistence.
///
/// The convention only knows "does the child's own type have a collection
/// property on the parent" - it cannot single out one non-FK field on a type
/// that also has real child fields, so it can't distinguish "no relationship
/// matches this specific field" from "no relationship matches any field on
/// this type." The case below uses a parent type with no such collection at
/// all, which the resolver correctly rejects.
/// </summary>
public class InjectionPathResolverTest
{
    [Fact]
    public void ParentRelationshipField_ForAStandardLookup_ReturnsTheNavigationProperty()
    {
        // Arrange
        PropertyInfo lookup = Field.Of<Contact>(x => x.AccountId);

        // Act
        PropertyInfo field = InjectionPathResolver.ParentRelationshipField(lookup);

        // Assert
        Assert.Equal(nameof(Contact.Account), field.Name);
    }

    [Fact]
    public void ParentRelationshipField_ForASelfLookup_ReturnsTheSelfNavigationProperty()
    {
        // Arrange
        PropertyInfo selfLookup = Field.Of<Account>(x => x.ParentId);

        // Act
        PropertyInfo field = InjectionPathResolver.ParentRelationshipField(selfLookup);

        // Assert
        Assert.Equal(nameof(Account.Parent), field.Name);
    }

    [Fact]
    public void ParentRelationshipField_ForANonReferenceField_Throws()
    {
        // Arrange
        PropertyInfo notALookup = Field.Of<Account>(x => x.Name);

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
        PropertyInfo field = InjectionPathResolver.ChildRelationshipField(parentType, Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Equal(nameof(Account.Contacts), field.Name);
    }

    [Fact]
    public void ChildRelationshipField_WhenNoChildRelationshipMatches_Throws()
    {
        // Arrange - Contact has no collection property of its own type
        PropertyInfo notAChildLookup = Field.Of<Contact>(x => x.Birthdate);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => InjectionPathResolver.ChildRelationshipField(typeof(Contact), notAChildLookup));

        // Assert
        Assert.NotNull(thrown);
    }
}
