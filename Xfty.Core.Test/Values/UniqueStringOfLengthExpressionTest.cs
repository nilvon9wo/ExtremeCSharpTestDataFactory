using System.Text.RegularExpressions;
using FluentAssertions;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

/// <summary>
/// Proves <see cref="UniqueStringOfLengthExpression"/> - Get produces
/// fixed-length, uppercase, unique values, counted separately per length.
///
/// Each test below uses lengths no other test in this class touches. The
/// per-length counter is a process-static Dictionary that - unlike Apex,
/// where statics reset before every test method - is not reset between
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
        List<string> produced = Enumerable.Range(0, 20).Select(_ => (string)expression.Get()!).ToList();

        // Assert
        produced.Distinct().Should().HaveCount(20);
        produced.Should().OnlyContain(value => value.Length == 37);
        produced.Should().OnlyContain(value => Regex.IsMatch(value, "^[A-Z]{37}$"));
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
        secondValue.Should().NotBe(firstValue);
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
        firstOfEachLength[0].Should().BeOfType<string>().Which.Should().HaveLength(43);
        firstOfEachLength[1].Should().BeOfType<string>().Which.Should().HaveLength(44);
    }
}
