using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

/// <summary>
/// Proves <see cref="CopyFromDescendantExpression"/> by building a
/// <see cref="DeferredGraph"/> directly - the Apex original also proved this
/// resolved through the DEFERRED flush, which isn't ported yet (see
/// csharp-port-idea.md); the expression's own logic doesn't depend on it.
/// </summary>
public class CopyFromDescendantExpressionTest
{
    [Fact]
    public void Get_WhenThereIsNoMatchingChild_ReturnsNull()
    {
        // Arrange - the up-flow field points at a child relationship nothing generated
        DeferredGraph graph = new([new Account()], []);
        CopyFromDescendantExpression expression = new(
            Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Contact>(nameof(Contact.Department)));

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
            [new DeferredGraphParentLink(0, 1, Field.Of<Contact>(nameof(Contact.AccountId)))]);
        CopyFromDescendantExpression expression = new(
            Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Contact>(nameof(Contact.Department)));

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
            () => new CopyFromDescendantExpression(null!, Field.Of<Contact>(nameof(Contact.Department))));

        // Assert - a null field must be rejected at construction
        Assert.Contains("child lookup field", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }
}
