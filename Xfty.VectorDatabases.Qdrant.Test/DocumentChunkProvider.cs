using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>A demo Provider pairing a `Content` field with a `RandomVectorExpression`-generated embedding.</summary>
public sealed class DocumentChunkProvider() : SimpleRecordProvider<DocumentChunk>(
    new MasterTemplate<DocumentChunk>(x => x.Id)
        .Put(x => x.Content, new IncrementingStringExpression("chunk"))
        .Put(x => x.Embedding, new RandomVectorExpression(dimensions: 16)));
