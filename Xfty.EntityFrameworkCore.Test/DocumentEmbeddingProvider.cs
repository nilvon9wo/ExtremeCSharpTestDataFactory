using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>A demo Provider pairing a `Content` field with a pgvector-mapped embedding.</summary>
public sealed class DocumentEmbeddingProvider() : SimpleRecordProvider<DocumentEmbedding>(
    new MasterTemplate<DocumentEmbedding>(x => x.Id)
        .Put(x => x.Content, new IncrementingStringExpression("chunk"))
        .Put(x => x.Embedding, new RandomPgVectorExpression(DocumentEmbedding.EmbeddingDimensions)));
