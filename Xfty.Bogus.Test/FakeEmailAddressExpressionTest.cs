namespace Net.Nowhereatall.Xfty.Bogus.Test;

/// <summary>Proves <see cref="FakeEmailAddressExpression"/> - Get produces well-formed, varied addresses.</summary>
public class FakeEmailAddressExpressionTest
{
    [Fact]
    public void Get_ForManyCalls_ProducesWellFormedVariedAddresses()
    {
        // Arrange
        FakeEmailAddressExpression expression = new();

        // Act
        List<string> produced = [.. Enumerable.Range(0, 25).Select(_ => (string)expression.Get()!)];

        // Assert
        Assert.All(produced, email => Assert.Contains('@', email));
        Assert.True(produced.Distinct().Count() > 1, "expected varied addresses across many calls");
    }
}
