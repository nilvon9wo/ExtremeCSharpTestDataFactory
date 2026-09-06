using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Values;

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
        Assert.Equal(["Account 1", "Account 2", "Account 3"], sequence);
    }

    [Fact]
    public void Get_WithNoSeparator_JoinsThePrefixAndCounter()
    {
        // Arrange
        IncrementingStringExpression expression = new("ACME", IncrementingStringExpression.DontSeparatePrefix);

        // Act
        object?[] sequence = [expression.Get(), expression.Get()];

        // Assert
        Assert.Equal(["ACME1", "ACME2"], sequence);
    }

    [Fact]
    public void Get_ForTwoInstances_CountsIndependently()
    {
        // Arrange
        IncrementingStringExpression first = new("P");
        IncrementingStringExpression second = new("P");
        _ = first.Get();
        _ = first.Get();

        // Act
        object? secondsFirstValue = second.Get();

        // Assert - each instance counts independently
        Assert.Equal("P 1", secondsFirstValue);
    }
}
