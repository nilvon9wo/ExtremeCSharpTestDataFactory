namespace Net.NowhereAtAll.Xfty.Bogus.Test;

/// <summary>Proves <see cref="FakeFullNameExpression"/> - Get produces varied, well-formed full names.</summary>
public class FakeFullNameExpressionTest
{
    [Fact]
    public void Get_ForManyCalls_ProducesVariedNonEmptyNames()
    {
        // Arrange
        FakeFullNameExpression expression = new();

        // Act
        List<string> produced = [.. Enumerable.Range(0, 25).Select(_ => (string)expression.Get()!)];

        // Assert
        Assert.All(produced, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.True(produced.Distinct().Count() > 1, "expected varied names across many calls");
    }
}
