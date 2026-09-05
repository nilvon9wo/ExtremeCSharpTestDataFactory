using Net.Nowhereatall.Xfty.Values;
using Net.Nowhereatall.Xfty.VectorDatabases;
using Pgvector;

namespace Net.Nowhereatall.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// Adapts <see cref="RandomVectorExpression"/>'s <c>float[]</c> output to
/// <see cref="Vector"/>, the type <c>Pgvector.EntityFrameworkCore</c> maps
/// onto a pgvector column - composing the two already-built packages
/// instead of writing a third random-vector generator.
/// </summary>
public sealed class RandomPgVectorExpression(int dimensions) : IValueExpression
{
    private readonly RandomVectorExpression inner = new(dimensions);

    public object Get() => new Vector((float[])this.inner.Get()!);
}
