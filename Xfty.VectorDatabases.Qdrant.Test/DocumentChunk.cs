namespace Net.Nowhereatall.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>
/// A minimal demo record for this package's own test - a chunk of source
/// text plus the embedding computed from it, the common shape a vector
/// database record takes.
/// </summary>
public sealed class DocumentChunk
{
    public Guid? Id { get; set; }

    public string? Content { get; set; }

    public float[]? Embedding { get; set; }
}
