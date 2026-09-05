namespace Net.Nowhereatall.Xfty.VectorDatabases.Test;

/// <summary>Proves <see cref="RandomVectorExpression"/> - Get produces the requested shape, range, and variety.</summary>
public class RandomVectorExpressionTest
{
    [Fact]
    public void Get_WithDimensions_ProducesAnArrayOfThatLength()
    {
        // Arrange
        RandomVectorExpression expression = new(dimensions: 128);

        // Act
        float[] vector = (float[])expression.Get()!;

        // Assert
        Assert.Equal(128, vector.Length);
    }

    [Fact]
    public void Get_WithDefaultRange_ProducesComponentsWithinNegativeOneToOne()
    {
        // Arrange
        RandomVectorExpression expression = new(dimensions: 64);

        // Act
        float[] vector = (float[])expression.Get()!;

        // Assert
        Assert.All(vector, component => Assert.InRange(component, -1f, 1f));
    }

    [Fact]
    public void Get_WithACustomRange_ProducesComponentsWithinThatRange()
    {
        // Arrange
        RandomVectorExpression expression = new(dimensions: 64, min: 10f, max: 20f);

        // Act
        float[] vector = (float[])expression.Get()!;

        // Assert
        Assert.All(vector, component => Assert.InRange(component, 10f, 20f));
    }

    [Fact]
    public void Get_ForManyCalls_ProducesVariedVectors()
    {
        // Arrange
        RandomVectorExpression expression = new(dimensions: 8);

        // Act
        List<float[]> produced = [.. Enumerable.Range(0, 10).Select(_ => (float[])expression.Get()!)];

        // Assert
        Assert.True(produced.Select(vector => vector[0]).Distinct().Count() > 1, "expected varied vectors across many calls");
    }
}
