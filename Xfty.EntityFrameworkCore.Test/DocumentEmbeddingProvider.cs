using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>A demo Provider pairing a `Content` field with a pgvector-mapped embedding.</summary>
public sealed class DocumentEmbeddingProvider : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<DocumentEmbedding>(x => x.Id)
        .Put(x => x.Content, new IncrementingStringExpression("chunk"))
        .Put(x => x.Embedding, new RandomPgVectorExpression(DocumentEmbedding.EmbeddingDimensions));

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}