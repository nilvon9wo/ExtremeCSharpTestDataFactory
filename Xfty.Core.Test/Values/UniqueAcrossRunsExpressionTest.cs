using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

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
        twoCalls[0].Should().BeOfType<string>().Which.Should().StartWith("u.").And.EndWith("@example.com");
        twoCalls[1].Should().NotBe(twoCalls[0], "the counter still moves within a run");
    }

    [Fact]
    public void Get_ForOneCall_CarriesAPerRunToken()
    {
        // Arrange - the token is what makes two persisted runs not collide
        UniqueAcrossRunsExpression expression = new("User Federation Id ", "");

        // Act
        object? value = expression.Get();

        // Assert - prefix, then digits from the token + counter (no "User Federation Id 1" literal)
        string stringValue = value.Should().BeOfType<string>().Which;
        stringValue.Should().StartWith("User Federation Id ");
        stringValue.Length.Should().BeGreaterThan("User Federation Id 1".Length);
    }
}
