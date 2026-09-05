using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Values;

/// <summary>
/// Proves <see cref="UniqueAcrossRunsExpression"/> - Get wraps a counter in a
/// prefix/suffix, and carries a per-run token so persisted runs don't collide.
/// </summary>
public class UniqueAcrossRunsExpressionTest
{
    [Fact]
    public void Get_ForTwoCalls_WrapsACounterInAPrefixAndSuffix()
    {
        // Arrange
        UniqueAcrossRunsExpression expression = new("u.", "@example.com");

        // Act
        object?[] twoCalls = [expression.Get(), expression.Get()];

        // Assert
        string firstValue = Assert.IsType<string>(twoCalls[0]);
        Assert.StartsWith("u.", firstValue);
        Assert.EndsWith("@example.com", firstValue);
        Assert.NotEqual(twoCalls[0], twoCalls[1]);
    }

    [Fact]
    public void Get_ForOneCall_CarriesAPerRunToken()
    {
        // Arrange - the token is what makes two persisted runs not collide
        UniqueAcrossRunsExpression expression = new("User Federation Id ", "");

        // Act
        object? value = expression.Get();

        // Assert - prefix, then digits from the token + counter (no "User Federation Id 1" literal)
        string stringValue = Assert.IsType<string>(value);
        Assert.StartsWith("User Federation Id ", stringValue);
        Assert.True(stringValue.Length > "User Federation Id 1".Length);
    }
}
