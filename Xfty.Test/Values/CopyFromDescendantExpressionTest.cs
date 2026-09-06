using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Values;

/// <summary>
/// Proves <see cref="CopyFromDescendantExpression"/> by building a
/// <see cref="DeferredGraph"/> directly - the expression's own logic doesn't
/// depend on how the graph was assembled. Resolved end to end through the
/// DEFERRED flush is proven in
/// ExContextAwareValuesTest.ReadingUpFromAChild_NeedsDeferredMode.
/// </summary>
public class CopyFromDescendantExpressionTest
{
    [Fact]
    public void Get_WhenThereIsNoMatchingChild_ReturnsNull()
    {
        // Arrange - the up-flow field points at a child relationship nothing generated
        DeferredGraph graph = new([new Account()], []);
        CopyFromDescendantExpression expression = new(
            Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department));

        // Act
        object? actualResult = expression.Get(graph, 0);

        // Assert - no child means null, not an error
        Assert.Null(actualResult);
    }

    [Fact]
    public void Get_WhenAChildMatches_ReadsTheFirstOnesValue()
    {
        // Arrange
        Account parent = new();
        Contact child = new() { Department = "Field Ops" };
        DeferredGraph graph = new(
            [parent, child],
            [new DepthBatchedInserterParentLink(childIndex: 1, parentIndex: 0, Field.Of<Contact>(x => x.AccountId))]);
        CopyFromDescendantExpression expression = new(
            Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department));

        // Act
        object? actualResult = expression.Get(graph, 0);

        // Assert
        Assert.Equal("Field Ops", actualResult);
    }

    [Fact]
    public void Constructor_WhenTheChildLookupFieldIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => new CopyFromDescendantExpression(null!, Field.Of<Contact>(x => x.Department)));

        // Assert - a null field must be rejected at construction
        Assert.Contains("cannot be null", thrown.Message);
    }

    [Fact]
    public void Constructor_WhenThePathIsTooShort_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => new CopyFromDescendantExpression([Field.Of<Contact>(x => x.Department)]));

        // Assert
        Assert.Contains("at least one child-lookup field", thrown.Message);
    }

    [Fact]
    public void Get_WithATwoHopPath_ReadsTheFieldFromAGrandchild()
    {
        // Arrange - Account -> Contact (hop 1) -> Case (hop 2), reading the Case's Subject
        Account grandparent = new();
        Contact parent = new();
        Case grandchild = new() { Subject = "Escalated" };
        DeferredGraph graph = new(
            [grandparent, parent, grandchild],
            [
                new DepthBatchedInserterParentLink(childIndex: 1, parentIndex: 0, Field.Of<Contact>(x => x.AccountId)),
                new DepthBatchedInserterParentLink(childIndex: 2, parentIndex: 1, Field.Of<Case>(x => x.ContactId)),
            ]);
        CopyFromDescendantExpression expression = new(
        [
            Field.Of<Contact>(x => x.AccountId),
            Field.Of<Case>(x => x.ContactId),
            Field.Of<Case>(x => x.Subject),
        ]);

        // Act
        object? actualResult = expression.Get(graph, 0);

        // Assert
        Assert.Equal("Escalated", actualResult);
    }

    [Fact]
    public void Get_WithATwoHopPath_WhenTheFirstHopHasNoMatch_ReturnsNull()
    {
        // Arrange - no Contact generated under the Account at all
        DeferredGraph graph = new([new Account()], []);
        CopyFromDescendantExpression expression = new(
        [
            Field.Of<Contact>(x => x.AccountId),
            Field.Of<Case>(x => x.ContactId),
            Field.Of<Case>(x => x.Subject),
        ]);

        // Act
        object? actualResult = expression.Get(graph, 0);

        // Assert - missing an intermediate hop is null, not an error
        Assert.Null(actualResult);
    }

    [Fact]
    public void Get_WithATwoHopPath_WhenTheSecondHopHasNoMatch_ReturnsNull()
    {
        // Arrange - a Contact exists under the Account, but no Case exists under that Contact
        Account grandparent = new();
        Contact parent = new();
        DeferredGraph graph = new(
            [grandparent, parent],
            [new DepthBatchedInserterParentLink(childIndex: 1, parentIndex: 0, Field.Of<Contact>(x => x.AccountId))]);
        CopyFromDescendantExpression expression = new(
        [
            Field.Of<Contact>(x => x.AccountId),
            Field.Of<Case>(x => x.ContactId),
            Field.Of<Case>(x => x.Subject),
        ]);

        // Act
        object? actualResult = expression.Get(graph, 0);

        // Assert
        Assert.Null(actualResult);
    }
}
