namespace Net.NowhereAtAll.Xfty.VectorDatabases;

/// <summary>
/// Embedding dimensionality for popular models, so a test doesn't have to
/// hardcode or look up a magic number:
/// <c>new RandomVectorExpression(KnownEmbeddingDimensions.OpenAiTextEmbedding3Small)</c>
/// instead of <c>new RandomVectorExpression(1536)</c>. Not exhaustive, and
/// not tied to any particular provider's SDK - just the number a real
/// embedding of that name would have. Pass a literal for anything not
/// listed here.
/// </summary>
public static class KnownEmbeddingDimensions
{
    public const int OpenAiTextEmbeddingAda002 = 1536;
    public const int OpenAiTextEmbedding3Small = 1536;
    public const int OpenAiTextEmbedding3Large = 3072;
    public const int CohereEmbedV3 = 1024;
    public const int GoogleTextEmbedding004 = 768;
}
