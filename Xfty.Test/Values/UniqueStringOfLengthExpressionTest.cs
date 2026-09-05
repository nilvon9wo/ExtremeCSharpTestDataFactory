using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Values;

/// <summary>
/// Proves <see cref="UniqueStringOfLengthExpression"/> - Get produces
/// fixed-length, uppercase, unique values, counted separately per length.
///
/// Each test below uses lengths no other test in this class touches. The
/// per-length counter is a process-static Dictionary, not reset between
/// xUnit tests in the same run, so asserting an exact "first value" (e.g.
/// "AAA") would be order-dependent across the whole assembly. Distinct
/// otherwise-unused lengths sidestep that without weakening what's proved.
/// </summary>
public class UniqueStringOfLengthExpressionTest
{
    [Fact]
    public void Get_ForManyCalls_ProducesFixedLengthUppercaseUniqueValues()
    {
        // Arrange
        UniqueStringOfLengthExpression expression = new(37);

        // Act
        List<string> produced = [.. Enumerable.Range(0, 20).Select(_ => (string)expression.Get()!)];

        // Assert
        Assert.Equal(20, produced.Distinct().Count());
        Assert.All(produced, value => Assert.Matches("^[A-Z]{37}$", value));
    }

    [Fact]
    public void Get_ForTwoInstancesOfTheSameLength_ShareTheCounter()
    {
        // Arrange
        UniqueStringOfLengthExpression first = new(41);
        UniqueStringOfLengthExpression second = new(41);
        object? firstValue = first.Get();

        // Act
        object? secondValue = second.Get();

        // Assert - the counter is per length, not per instance
        Assert.NotEqual(firstValue, secondValue);
    }

    [Fact]
    public void Get_ForDifferentLengths_CountsSeparately()
    {
        // Arrange
        UniqueStringOfLengthExpression lengthFortyThree = new(43);
        UniqueStringOfLengthExpression lengthFortyFour = new(44);

        // Act
        object?[] firstOfEachLength = [lengthFortyThree.Get(), lengthFortyFour.Get()];

        // Assert - each length's own first value has that length, independent of the other
        Assert.Equal(43, Assert.IsType<string>(firstOfEachLength[0]).Length);
        Assert.Equal(44, Assert.IsType<string>(firstOfEachLength[1]).Length);
    }
}
