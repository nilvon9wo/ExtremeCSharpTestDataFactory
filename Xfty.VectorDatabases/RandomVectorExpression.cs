using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.VectorDatabases;

/// <summary>
/// An <see cref="IValueExpression"/> filling a vector-database record's
/// embedding field with a fixed-length <see cref="float"/> array of
/// independent random values - structurally a vector, not a semantically
/// meaningful embedding. A test asserting a real nearest-neighbor
/// relationship needs vectors informed by its own domain; see
/// docs/roadmap/vector-databases.md.
/// </summary>
public sealed class RandomVectorExpression(
    int dimensions,
    float min = RandomVectorExpression.DefaultMin,
    float max = RandomVectorExpression.DefaultMax,
    bool normalize = false
) : IValueExpression
{
    private const float DefaultMin = -1f;
    private const float DefaultMax = 1f;

    private readonly int _dimensions = dimensions;
    private readonly float _min = min;
    private readonly float _max = max;
    private readonly bool _normalize = normalize;

    public object Get() => this.GenerateVector();

    private float[] GenerateVector()
    {
        float[] vector = [.. Enumerable.Range(0, this._dimensions).Select(_ => this.NextComponent())];
        return this._normalize
            ? Normalize(vector)
            : vector;
    }

    private float NextComponent() => this._min + ((float)Random.Shared.NextDouble() * (this._max - this._min));

    private static float[] Normalize(float[] vector)
    {
        double magnitude = Math.Sqrt(vector.Sum(component => (double)component * component));
        return magnitude > 0
            ? [.. vector.Select(component => (float)(component / magnitude))]
            : vector;
    }
}