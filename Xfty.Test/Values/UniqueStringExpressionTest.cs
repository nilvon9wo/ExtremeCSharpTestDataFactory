using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Values;

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
        List<object> produced = [.. expressions.Select(expression => expression.Get())];

        // Assert - the static counter keeps values distinct across instances
        Assert.Equal(25, produced.Distinct().Count());
    }

    [Fact]
    public void Get_ForAUniqueString_StartsWithTheSuppliedPrefix()
    {
        // Arrange
        UniqueStringExpression expression = new("Widget");

        // Act
        object? value = expression.Get();

        // Assert
        Assert.StartsWith("Widget ", Assert.IsType<string>(value));
    }
}
