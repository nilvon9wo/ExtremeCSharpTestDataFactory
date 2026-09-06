using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.Test;

/// <summary>
/// Proves the exact usage shown in this package's own README.md and
/// docs/use/vector-databases.md - a Master Template entry wired straight to
/// <see cref="RandomVectorExpression"/> via a <see cref="KnownEmbeddingDimensions"/>
/// constant, not just the expression in isolation (see
/// RandomVectorExpressionTest for that).
/// </summary>
file sealed class DocumentChunkProvider() : SimpleRecordProvider<DocumentChunk>(
    new MasterTemplate<DocumentChunk>(x => x.Id)
    {
        [x => x.Embedding] = new RandomVectorExpression(KnownEmbeddingDimensions.OpenAiTextEmbedding3Small),
    })
{
}

public class VectorDatabasesReadmeExampleTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(DocumentChunk))] = new DocumentChunkProvider(),
        });

    [Fact]
    public async Task Supply_UsingRandomVectorExpressionInAMasterTemplate_ProducesAVectorOfTheDeclaredDimension()
    {
        // Arrange
        IProviderLookup lookup = Lookup();

        // Act
        DocumentChunk result = (DocumentChunk)await new RecordProvider(typeof(DocumentChunk), lookup)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert
        Assert.NotNull(result.Embedding);
        Assert.Equal(KnownEmbeddingDimensions.OpenAiTextEmbedding3Small, result.Embedding!.Length);
    }
}
