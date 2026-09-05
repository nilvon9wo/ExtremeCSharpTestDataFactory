using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Values;

/// <summary>Proves <see cref="IncrementingDecimalExpression"/> - Get returns ascending decimals, 1, 2, ...</summary>
public class IncrementingDecimalExpressionTest
{
    [Fact]
    public void Get_ForAnIncrementingDecimal_ReturnsAscendingDecimals()
    {
        // Arrange
        IncrementingDecimalExpression expression = new();

        // Act
        object?[] twoCalls = [expression.Get(), expression.Get()];

        // Assert
        Assert.Equal([1m, 2m], twoCalls);
    }
}
