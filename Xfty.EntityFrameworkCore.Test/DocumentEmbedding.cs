using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace Net.Nowhereatall.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// A minimal demo record proving the pgvector option from
/// docs/roadmap/vector-databases.md: a vector-shaped column that flows
/// through the existing, unmodified <see cref="EfPersistenceGateway"/> -
/// no new gateway code, just an entity shape and the Pgvector.EntityFrameworkCore
/// package. See <see cref="EmbeddingDimensions"/> for why the column type
/// hardcodes a dimension count.
/// </summary>
public sealed class DocumentEmbedding
{
    public const int EmbeddingDimensions = 8;

    public string? Id { get; set; }

    public string? Content { get; set; }

    // The column type string can't reference EmbeddingDimensions directly -
    // attribute arguments must be compile-time constants, and interpolating
    // a const int into one isn't. Keep this literal in sync with it by hand.
    [Column(TypeName = "vector(8)")]
    public Vector? Embedding { get; set; }
}
