using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.VectorDatabases;

/// <summary>
/// An <see cref="IValueExpression"/> filling a vector-database record's
/// embedding field with a fixed-length <see cref="float"/> array of
/// independent random values - structurally a vector, not a semantically
/// meaningful embedding. A test asserting a real nearest-neighbor
/// relationship needs vectors informed by its own domain; see
/// docs/roadmap/vector-databases.md.
/// </summary>
public sealed class RandomVectorExpression(int dimensions, float min = RandomVectorExpression.DefaultMin, float max = RandomVectorExpression.DefaultMax, bool normalize = false) : IValueExpression
{
    private const float DefaultMin = -1f;
    private const float DefaultMax = 1f;

    private readonly int dimensions = dimensions;
    private readonly float min = min;
    private readonly float max = max;
    private readonly bool normalize = normalize;

    public object Get() => this.GenerateVector();

    private float[] GenerateVector()
    {
        float[] vector = [.. Enumerable.Range(0, this.dimensions).Select(_ => this.NextComponent())];
        return this.normalize
            ? Normalize(vector)
            : vector;
    }

    private float NextComponent() => this.min + ((float)Random.Shared.NextDouble() * (this.max - this.min));

    private static float[] Normalize(float[] vector)
    {
        double magnitude = Math.Sqrt(vector.Sum(component => (double)component * component));
        return magnitude > 0
            ? [.. vector.Select(component => (float)(component / magnitude))]
            : vector;
    }
}
