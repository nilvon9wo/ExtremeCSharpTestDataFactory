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
public sealed class RandomVectorExpression : IValueExpression
{
    private const float DefaultMin = -1f;
    private const float DefaultMax = 1f;

    private readonly int dimensions;
    private readonly float min;
    private readonly float max;

    public RandomVectorExpression(int dimensions, float min = DefaultMin, float max = DefaultMax)
    {
        this.dimensions = dimensions;
        this.min = min;
        this.max = max;
    }

    public object Get() => this.GenerateVector();

    private float[] GenerateVector() =>
        [.. Enumerable.Range(0, this.dimensions).Select(_ => this.NextComponent())];

    private float NextComponent() => this.min + ((float)Random.Shared.NextDouble() * (this.max - this.min));
}
