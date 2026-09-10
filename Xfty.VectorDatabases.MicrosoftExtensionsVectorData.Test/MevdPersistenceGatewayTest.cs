using global::Qdrant.Client;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Testcontainers.Qdrant;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test;

/// <summary>
/// PREVIEW / proof-of-concept - see ../Xfty.VectorDatabases.MicrosoftExtensionsVectorData/README.md
/// for known assumptions and accepted risks.
///
/// Proves <see cref="MevdPersistenceGateway"/> against a real Qdrant
/// container, started with Docker via Testcontainers - Qdrant here is only
/// one concrete example of a <see cref="VectorStore"/>; the gateway itself
/// has no idea which provider it's talking to. Requires a running Docker
/// daemon; skips (rather than fails) when one isn't reachable.
/// </summary>
[Trait("Category", "Docker")]
public sealed class MevdPersistenceGatewayTest : IAsyncLifetime
{
    private QdrantContainer? _container;
    private QdrantClient? _client;
    private bool _dockerAvailable = true;

    public async ValueTask InitializeAsync()
    {
        try
        {
            this._container = new QdrantBuilder("qdrant/qdrant:v1.18.2").Build();
            await this._container.StartAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Docker is not reachable from this machine right now - skip this tier rather than fail the build.
            this._dockerAvailable = false;
            return;
        }

        this._client = new QdrantClient(new Uri(this._container.GetGrpcConnectionString()));
    }

    public async ValueTask DisposeAsync()
    {
        this._client?.Dispose();

        if (this._container is not null)
        {
            await this._container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task Supply_InNowMode_AgainstARealVectorStore_ActuallyInsertsARecord()
    {
        Assert.SkipUnless(this._dockerAvailable, "Docker is not reachable from this machine - start Docker Desktop to run this tier.");

        // Arrange - a QdrantVectorStore here is just one VectorStore among many the gateway could take.
        VectorStore vectorStore = new QdrantVectorStore(this._client!, ownsClient: false);
        RecordProvider provider = new RecordProvider(typeof(DocumentChunk), new DemoProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new MevdPersistenceGateway(vectorStore));

        // Act
        DocumentChunk result = (DocumentChunk)await provider.Supply().ConfigureAwait(true);

        // Assert
        _ = Assert.NotNull(result.Id);
        Assert.NotNull(result.Embedding);
        Assert.Equal(16, result.Embedding!.Length);
    }
}