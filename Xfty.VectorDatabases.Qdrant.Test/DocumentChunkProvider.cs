using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>A demo Provider pairing a `Content` field with a `RandomVectorExpression`-generated embedding.</summary>
public sealed class DocumentChunkProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate<DocumentChunk>(x => x.Id)
        .Put(x => x.Content, new IncrementingStringExpression("chunk"))
        .Put(x => x.Embedding, new RandomVectorExpression(dimensions: 16));

    public PropertyInfo PrimaryTargetField => this._template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this._template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
