namespace Net.Nowhereatall.Xfty.VectorDatabases.Test;

/// <summary>
/// A minimal demo record for this package's own README/doc example - a
/// chunk of source text plus the embedding computed from it, the common
/// shape a vector database record takes. Illustrative only; this package
/// itself has no dependency on any particular record shape.
/// </summary>
public sealed class DocumentChunk
{
    public string? Id { get; set; }

    public string? Content { get; set; }

    public float[]? Embedding { get; set; }
}
