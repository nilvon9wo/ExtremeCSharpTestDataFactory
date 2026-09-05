namespace Net.Nowhereatall.Xfty.Bogus.Test;

/// <summary>Proves <see cref="FakeParagraphExpression"/> - Get produces multi-sentence, varied text.</summary>
public class FakeParagraphExpressionTest
{
    [Fact]
    public void Get_ForManyCalls_ProducesMultiSentenceVariedText()
    {
        // Arrange
        FakeParagraphExpression expression = new(sentenceCount: 3);

        // Act
        List<string> produced = [.. Enumerable.Range(0, 10).Select(_ => (string)expression.Get()!)];

        // Assert
        Assert.All(produced, paragraph => Assert.True(paragraph.Split('.').Length > 2, paragraph));
        Assert.True(produced.Distinct().Count() > 1, "expected varied paragraphs across many calls");
    }

    [Fact]
    public void Get_WithDefaultConstructor_ProducesNonEmptyText()
    {
        // Arrange
        FakeParagraphExpression expression = new();

        // Act
        string produced = (string)expression.Get()!;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(produced));
    }
}
