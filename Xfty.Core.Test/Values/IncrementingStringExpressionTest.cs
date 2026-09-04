using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

/// <summary>
/// Proves <see cref="IncrementingStringExpression"/> - Get produces "prefix N"
/// by default, "prefixN" when told not to separate, and each instance counts
/// independently.
/// </summary>
public class IncrementingStringExpressionTest
{
    [Fact]
    public void Get_ByDefault_SeparatesThePrefixAndCounter()
    {
        // Arrange
        IncrementingStringExpression expression = new("Account");

        // Act
        object?[] sequence = [expression.Get(), expression.Get(), expression.Get()];

        // Assert
        sequence.Should().Equal("Account 1", "Account 2", "Account 3");
    }

    [Fact]
    public void Get_WithNoSeparator_JoinsThePrefixAndCounter()
    {
        // Arrange
        IncrementingStringExpression expression = new("ACME", IncrementingStringExpression.DontSeparatePrefix);

        // Act
        object?[] sequence = [expression.Get(), expression.Get()];

        // Assert
        sequence.Should().Equal("ACME1", "ACME2");
    }

    [Fact]
    public void Get_ForTwoInstances_CountsIndependently()
    {
        // Arrange
        IncrementingStringExpression first = new("P");
        IncrementingStringExpression second = new("P");
        first.Get();
        first.Get();

        // Act
        object? secondsFirstValue = second.Get();

        // Assert - each instance counts independently
        secondsFirstValue.Should().Be("P 1");
    }
}
