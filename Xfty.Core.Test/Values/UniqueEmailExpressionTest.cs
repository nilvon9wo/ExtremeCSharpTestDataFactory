using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

/// <summary>Proves <see cref="UniqueEmailExpression"/> - Get produces well-formed, unique addresses.</summary>
public class UniqueEmailExpressionTest
{
    [Fact]
    public void Get_ForManyCalls_ProducesWellFormedUniqueAddresses()
    {
        // Arrange - one call per instance, mirroring how callers actually use this
        IEnumerable<UniqueEmailExpression> expressions = Enumerable.Range(0, 25)
            .Select(_ => new UniqueEmailExpression("test.user"));

        // Act
        List<string> produced = expressions.Select(expression => (string)expression.Get()!).ToList();

        // Assert
        Assert.Equal(25, produced.Distinct().Count());
        Assert.All(produced, email => Assert.True(email.StartsWith("test.user") && email.EndsWith("@example.com"), email));
    }
}
