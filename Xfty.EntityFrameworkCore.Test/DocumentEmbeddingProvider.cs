using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.EntityFrameworkCore.Test;

/// <summary>A demo Provider pairing a `Content` field with a pgvector-mapped embedding.</summary>
public sealed class DocumentEmbeddingProvider() : SimpleRecordProvider<DocumentEmbedding>(
    new MasterTemplate<DocumentEmbedding>(x => x.Id)
        .Put(x => x.Content, new IncrementingStringExpression("chunk"))
        .Put(x => x.Embedding, new RandomPgVectorExpression(DocumentEmbedding.EmbeddingDimensions)));
