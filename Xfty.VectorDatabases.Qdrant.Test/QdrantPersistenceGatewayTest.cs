using global::Qdrant.Client;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Testcontainers.Qdrant;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.Qdrant.Test;

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
    public async Task Supply_InNowMode_AgainstARealQdrantContainer_ActuallyInsertsARecord()
    {
        Assert.SkipUnless(this._dockerAvailable, "Docker is not reachable from this machine - start Docker Desktop to run this tier.");

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(DocumentChunk), new DemoProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new QdrantPersistenceGateway(this._client!));

        // Act
        DocumentChunk result = (DocumentChunk)await provider.Supply().ConfigureAwait(true);

        // Assert
        _ = Assert.NotNull(result.Id);
        Assert.NotNull(result.Embedding);
        Assert.Equal(16, result.Embedding!.Length);
    }
}