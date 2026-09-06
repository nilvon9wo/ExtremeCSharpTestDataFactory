using System.Reflection;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Enrichment;

namespace Net.NowhereAtAll.Xfty.Test.Enrichment;

/// <summary>
/// Proves PathKey - a comparable string key for a relationship path. The key
/// format includes each field's declaring type, needed since reflection
/// PropertyInfo tokens are not globally unique - two different record types
/// can both have an "Id"-named property - so this is tested behaviourally
/// (equal paths produce equal keys, different paths do not) rather than
/// against one fixed literal string.
/// </summary>
public class PathKeyTest
{
    [Fact]
    public void Of_JoinsTheFieldsPositionally_SoTheSamePathProducesTheSameKey()
    {
        // Arrange
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)];
        List<PropertyInfo> samePath = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)];

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
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)];
        List<PropertyInfo> reversedFields = [Field.Of<Account>(x => x.Id), Field.Of<Contact>(x => x.Id)];

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
