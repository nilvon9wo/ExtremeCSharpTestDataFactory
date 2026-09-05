using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Values;

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
        Assert.Contains("child lookup field", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }
}
