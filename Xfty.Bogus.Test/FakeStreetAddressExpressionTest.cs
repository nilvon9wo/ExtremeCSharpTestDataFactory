namespace Net.NowhereAtAll.Xfty.Bogus.Test;

/// <summary>Proves <see cref="FakeStreetAddressExpression"/> - Get produces varied, non-empty addresses.</summary>
public class FakeStreetAddressExpressionTest
{
    [Fact]
    public void Get_ForManyCalls_ProducesVariedNonEmptyAddresses()
    {
        // Arrange
        FakeStreetAddressExpression expression = new();

        // Act
        List<string> produced = [.. Enumerable.Range(0, 25).Select(_ => (string)expression.Get()!)];

        // Assert
        Assert.All(produced, address => Assert.False(string.IsNullOrWhiteSpace(address)));
        Assert.True(produced.Distinct().Count() > 1, "expected varied addresses across many calls");
    }
}
