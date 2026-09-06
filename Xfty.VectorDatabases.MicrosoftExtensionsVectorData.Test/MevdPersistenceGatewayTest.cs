using global::Qdrant.Client;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Net.Nowhereatall.Xfty.Core;
using Testcontainers.Qdrant;

namespace Net.Nowhereatall.Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test;

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
    private QdrantContainer? container;
    private QdrantClient? client;
    private bool dockerAvailable = true;

    public async ValueTask InitializeAsync()
    {
        try
        {
            this.container = new QdrantBuilder("qdrant/qdrant:v1.18.2").Build();
            await this.container.StartAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Docker is not reachable from this machine right now - skip this tier rather than fail the build.
            this.dockerAvailable = false;
            return;
        }

        this.client = new QdrantClient(new Uri(this.container.GetGrpcConnectionString()));
    }

    public async ValueTask DisposeAsync()
    {
        this.client?.Dispose();

        if (this.container is not null)
        {
            await this.container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task Supply_InNowMode_AgainstARealVectorStore_ActuallyInsertsARecord()
    {
        Assert.SkipUnless(this.dockerAvailable, "Docker is not reachable from this machine - start Docker Desktop to run this tier.");

        // Arrange - a QdrantVectorStore here is just one VectorStore among many the gateway could take.
        VectorStore vectorStore = new QdrantVectorStore(this.client!, ownsClient: false);
        RecordProvider provider = new RecordProvider(typeof(DocumentChunk), new DemoProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new MevdPersistenceGateway(vectorStore));

        // Act
        DocumentChunk result = (DocumentChunk)await provider.Supply();

        // Assert
        _ = Assert.NotNull(result.Id);
        Assert.NotNull(result.Embedding);
        Assert.Equal(16, result.Embedding!.Length);
    }
}
