using System.Reflection;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>
/// Proves PathKey - a comparable string key for a relationship path. This
/// port's key format includes each field's declaring type (needed since,
/// unlike Apex's globally-unique SObjectField tokens, two different record
/// types can both have an "Id"-named property) - tested behaviourally
/// (equal paths produce equal keys, different paths do not) rather than
/// against Apex's literal "AccountId&gt;ParentId&gt;" string.
/// </summary>
public class PathKeyTest
{
    [Fact]
    public void Of_JoinsTheFieldsPositionally_SoTheSamePathProducesTheSameKey()
    {
        // Arrange
        List<PropertyInfo> path = [Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Account>(nameof(Account.ParentId))];
        List<PropertyInfo> samePath = [Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Account>(nameof(Account.ParentId))];

        // Act
        string key = PathKey.Of(path);
        string sameKey = PathKey.Of(samePath);

        // Assert
        Assert.Equal(sameKey, key);
        Assert.Contains("AccountId", key);
        Assert.Contains("ParentId", key);
    }

    [Fact]
    public void Of_ForADifferentlyOrderedPath_ProducesADifferentKey()
    {
        // Arrange
        List<PropertyInfo> path = [Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Account>(nameof(Account.ParentId))];
        List<PropertyInfo> reversedFields = [Field.Of<Account>(nameof(Account.Id)), Field.Of<Contact>(nameof(Contact.Id))];

        // Act
        string key = PathKey.Of(path);
        string reversedKey = PathKey.Of(reversedFields);

        // Assert
        Assert.NotEqual(reversedKey, key);
    }

    [Fact]
    public void Of_ForAnEmptyPath_IsEmpty()
    {
        // Arrange
        List<PropertyInfo> emptyPath = [];

        // Act
        string key = PathKey.Of(emptyPath);

        // Assert
        Assert.Equal(string.Empty, key);
    }

    [Fact]
    public void Of_ForANullPath_IsEmpty()
    {
        // Arrange
        List<PropertyInfo>? nullPath = null;

        // Act
        string key = PathKey.Of(nullPath);

        // Assert
        Assert.Equal(string.Empty, key);
    }
}
