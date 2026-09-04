using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

/// <summary>
/// Proves <see cref="UniqueStringExpression"/> - Get never repeats, even
/// across instances, and every value starts with the supplied prefix.
/// </summary>
public class UniqueStringExpressionTest
{
    [Fact]
    public void Get_ForManyCalls_NeverRepeatsEvenAcrossInstances()
    {
        // Arrange - one call per instance, mirroring how callers actually use this
        IEnumerable<UniqueStringExpression> expressions = Enumerable.Range(0, 25)
            .Select(_ => new UniqueStringExpression("Prefix"));

        // Act
        List<object> produced = expressions.Select(expression => expression.Get()).ToList();

        // Assert - the static counter keeps values distinct across instances
        produced.Distinct().Should().HaveCount(25);
    }

    [Fact]
    public void Get_ForAUniqueString_StartsWithTheSuppliedPrefix()
    {
        // Arrange
        UniqueStringExpression expression = new("Widget");

        // Act
        object? value = expression.Get();

        // Assert
        value.Should().BeOfType<string>().Which.Should().StartWith("Widget ");
    }
}
