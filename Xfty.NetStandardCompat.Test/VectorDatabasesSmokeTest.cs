using Net.NowhereAtAll.Xfty.VectorDatabases;

namespace Net.NowhereAtAll.Xfty.NetStandardCompat.Test;

/// <summary>
/// Proves Xfty.VectorDatabases' own compiled copy of Xfty/Internal/SharedRandom.cs
/// (linked in via its own `&lt;Compile Include&gt;`, not a duplicated source
/// file - see that csproj's own comment) actually runs under a real
/// down-level runtime, not just compiles. Same reasoning as
/// <see cref="SmokeTest"/>, for the one netstandard2.0-only branch outside
/// core Xfty itself.
/// </summary>
public class VectorDatabasesSmokeTest
{
    [Fact]
    public void RandomVectorExpression_ProducesAVectorOfTheRequestedLength()
    {
        // Arrange
        RandomVectorExpression expression = new(KnownEmbeddingDimensions.CohereEmbedV3);

        // Act
        object result = expression.Get();

        // Assert - SharedRandom.Instance.NextDouble() ran (per component) without throwing
        float[] vector = Assert.IsType<float[]>(result);
        Assert.Equal(KnownEmbeddingDimensions.CohereEmbedV3, vector.Length);
        Assert.All(vector, component => Assert.InRange(component, -1f, 1f));
    }
}