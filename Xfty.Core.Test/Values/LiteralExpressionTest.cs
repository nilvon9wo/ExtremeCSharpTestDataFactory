using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

/// <summary>Proves <see cref="LiteralExpression"/> - Get always returns the same fixed value, null included.</summary>
public class LiteralExpressionTest
{
    [Fact]
    public void Get_ForAStringLiteral_ReturnsItUnchanged() =>
        AssertReturnsItself("Customer");

    [Fact]
    public void Get_ForAnIntLiteral_ReturnsItUnchanged() =>
        AssertReturnsItself(42);

    [Fact]
    public void Get_ForANullLiteral_ReturnsNull() =>
        AssertReturnsItself(null);

    [Fact]
    public void Get_ForALiteral_ReturnsTheSameValueEveryCall()
    {
        // Arrange
        LiteralExpression expression = new("constant");

        // Act
        object?[] twoCalls = [expression.Get(), expression.Get()];

        // Assert
        twoCalls.Should().Equal("constant", "constant");
    }

    private static void AssertReturnsItself(object? value)
    {
        // Arrange
        LiteralExpression expression = new(value);

        // Act
        object? result = expression.Get();

        // Assert
        result.Should().Be(value);
    }
}
