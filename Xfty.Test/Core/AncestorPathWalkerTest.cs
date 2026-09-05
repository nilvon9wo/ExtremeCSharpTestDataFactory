using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>Proves AncestorPathWalker - reading a field several relationship hops up a generated ancestor graph. Pure in-memory, no DML/SOQL.</summary>
public class AncestorPathWalkerTest
{
    [Fact]
    public void Read_ForAOneHopAncestorFieldAndARow_ReadsTheAlignedParentValue()
    {
        // Arrange - two Contacts, each with its own parent Account carrying a distinct name
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact(), new Contact()]);
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account { Name = "Row Zero" }, new Account { Name = "Row One" }]);
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)];

        // Act
        object? rowOneName = AncestorPathWalker.Read(bundle, path, 1);

        // Assert - the parent aligned with row 1
        Assert.Equal("Row One", rowOneName);
    }

    [Fact]
    public void Read_ForAMultiHopPath_DescendsThroughEveryLeadingRelationship()
    {
        // Arrange - Contact -> Account (sub-bundle) -> parent Account (list), only the deepest name set
        Bundle accountBundle = new();
        _ = accountBundle.Put<Account>(x => x.ParentId, [new Account { Name = "Grandparent" }]);
        Bundle bundle = new();
        _ = bundle.Put<Contact>(x => x.AccountId, accountBundle);
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.Name)];

        // Act
        object? grandparentName = AncestorPathWalker.Read(bundle, path, 0);

        // Assert
        Assert.Equal("Grandparent", grandparentName);
    }

    [Fact]
    public void Read_WhenAHopWasNotGenerated_ReturnsNull()
    {
        // Arrange - the relationship the path asks for was never put
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact()]);
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)];

        // Act
        object? missing = AncestorPathWalker.Read(bundle, path, 0);

        // Assert - an ungenerated ancestor reads as null, it does not throw
        Assert.Null(missing);
    }

    [Fact]
    public void Read_WhenTheRowIndexIsOutOfRange_ReturnsNull()
    {
        // Arrange - one parent, ask for row 5
        Bundle bundle = new();
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account { Name = "Only" }]);
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)];

        // Act
        object? outOfRange = AncestorPathWalker.Read(bundle, path, 5);

        // Assert
        Assert.Null(outOfRange);
    }

    [Fact]
    public void Read_WhenTheRowIndexIsNegative_ReturnsNull()
    {
        // Arrange
        Bundle bundle = new();
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account { Name = "Only" }]);
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)];

        // Act
        object? negative = AncestorPathWalker.Read(bundle, path, -1);

        // Assert
        Assert.Null(negative);
    }

    [Fact]
    public void Read_WhenThePathIsTooShortToWalk_Throws()
    {
        // Arrange
        List<PropertyInfo> justAField = [Field.Of<Account>(x => x.Name)];
        Bundle bundle = new();

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => AncestorPathWalker.Read(bundle, justAField, 0));

        // Assert - a path needs at least one relationship hop then the field to read
        Assert.NotNull(thrown);
    }

    [Fact]
    public void Read_WhenAPathStepIsNull_Throws()
    {
        // Arrange
        List<PropertyInfo> withNullStep = [Field.Of<Contact>(x => x.AccountId), null!];
        Bundle bundle = new();

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => AncestorPathWalker.Read(bundle, withNullStep, 0));

        // Assert - a null path step is rejected
        Assert.NotNull(thrown);
    }
}
