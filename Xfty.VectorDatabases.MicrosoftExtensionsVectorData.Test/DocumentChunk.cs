namespace Net.Nowhereatall.Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test;

/// <summary>
/// A minimal demo record for this package's own test - a chunk of source
/// text plus the embedding computed from it, the common shape a vector
/// database record takes. A `Guid` id here because Qdrant (the one
/// concrete <c>VectorStore</c> this test proves the gateway against)
/// requires one - a different backing provider might accept a `string` or
/// an `int` instead, which is exactly the point of testing against the
/// generic gateway rather than assuming one provider's rule everywhere.
/// </summary>
public sealed class DocumentChunk
{
    public Guid? Id { get; set; }

    public string? Content { get; set; }

    public float[]? Embedding { get; set; }
}
