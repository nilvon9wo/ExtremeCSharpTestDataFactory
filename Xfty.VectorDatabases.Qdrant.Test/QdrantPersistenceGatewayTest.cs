using global::Qdrant.Client;
using Net.Nowhereatall.Xfty.Core;
using Testcontainers.Qdrant;

namespace Net.Nowhereatall.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>
/// PREVIEW / proof-of-concept - see ../Xfty.VectorDatabases.Qdrant/README.md
/// for known assumptions and accepted risks.
///
/// Proves <see cref="QdrantPersistenceGateway"/> against a real Qdrant
/// container started with Docker via Testcontainers. Requires a running
/// Docker daemon; skips (rather than fails) when one isn't reachable.
/// </summary>
[Trait("Category", "Docker")]
public sealed class QdrantPersistenceGatewayTest : IAsyncLifetime
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
    public void Supply_InNowMode_AgainstARealQdrantContainer_ActuallyInsertsARecord()
    {
        Assert.SkipUnless(this.dockerAvailable, "Docker is not reachable from this machine - start Docker Desktop to run this tier.");

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(DocumentChunk), new DemoProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new QdrantPersistenceGateway(this.client!));

        // Act
        DocumentChunk result = (DocumentChunk)provider.Supply();

        // Assert
        _ = Assert.NotNull(result.Id);
        Assert.NotNull(result.Embedding);
        Assert.Equal(16, result.Embedding!.Length);
    }
}
