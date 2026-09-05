using Net.Nowhereatall.Xfty.Core;
using System.Reflection;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Values;
using NSubstitute;

namespace Net.Nowhereatall.Xfty.Test.Values;

/// <summary>
/// Proves <see cref="CopyFromSiblingExpression"/>. The Apex original also
/// proved this driven end-to-end through a Provider's put(...)/supply() -
/// that needs the engine (the ancestor generator, plain-value filler, etc.),
/// not yet ported (see csharp-port-idea.md); these tests exercise the same
/// mechanism by building the <see cref="GenerationContext"/> directly.
/// </summary>
public class CopyFromSiblingExpressionTest
{
    private static readonly IProviderLookup Lookup = Substitute.For<IProviderLookup>();

    [Fact]
    public void Get_TakesTheSiblingsPlainValue()
    {
        // Arrange - reading a sibling only makes sense while a context-aware value is being generated
        Account record = new() { Site = "Berlin" };
        GenerationContext context = new GenerationContext(Lookup, InsertMode.Mock, InsertInclusivity.None)
            .ForRecord(record, new Bundle(), 0)
            .ForValueField(Field.Of<Account>(nameof(Account.Description)), new HashSet<PropertyInfo>());
        CopyFromSiblingExpression expression = new(Field.Of<Account>(nameof(Account.Site)));

        // Act
        object? actualResult = expression.Get(context);

        // Assert
        Assert.Equal("Berlin", actualResult);
    }

    [Fact]
    public void Get_WhenTheSiblingGeneratedToNull_YieldsNullWithoutThrowing()
    {
        // Arrange - Site is context-aware and already resolved to null (absent from "pending")
        Account record = new() { Site = null };
        GenerationContext context = new GenerationContext(Lookup, InsertMode.Mock, InsertInclusivity.None)
            .ForRecord(record, new Bundle(), 0)
            .ForValueField(Field.Of<Account>(nameof(Account.Description)), new HashSet<PropertyInfo>());
        CopyFromSiblingExpression expression = new(Field.Of<Account>(nameof(Account.Site)));

        // Act
        object? actualResult = expression.Get(context);

        // Assert - copying a generated-null sibling yields null, and does not throw
        Assert.Null(actualResult);
    }

    [Fact]
    public void Constructor_WhenTheFieldIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => new CopyFromSiblingExpression(null!));

        // Assert
        Assert.Contains("source field", thrown.Message);
    }

    [Fact]
    public void Type_IsAContextAwareExpressionNotAPlainValueExpression()
    {
        // Arrange - nothing to arrange

        // Act
        object expression = new CopyFromSiblingExpression(Field.Of<Account>(nameof(Account.Name)));

        // Assert - a context-aware value, not a plain one: no misleading no-arg Get() to call
        Assert.False(expression is IValueExpression);
        Assert.True(expression is IContextAwareExpression);
    }

    [Fact]
    public void Get_WhenRunOutsideTheValuePass_Throws()
    {
        // Arrange
        GenerationContext baseContext = new(Lookup, InsertMode.Mock, InsertInclusivity.None);
        CopyFromSiblingExpression expression = new(Field.Of<Account>(nameof(Account.Name)));

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => expression.Get(baseContext));

        // Assert - there is no record being built
        Assert.Contains("context-aware value is being generated", thrown.Message);
    }
}
