using Qdrant.Client;
using Testcontainers.Qdrant;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>
/// One real Qdrant container (Docker, via Testcontainers) shared by every
/// test in a class, rather than one per test method. When Docker is not
/// reachable, <see cref="IsDockerAvailable"/> is false and the Docker-backed
/// tests skip rather than fail.
/// </summary>
public sealed class QdrantContainerFixture : IAsyncLifetime
{
    private const string Image = "qdrant/qdrant:v1.18.2";

    private QdrantContainer? _container;
    private QdrantClient? _client;

    public bool IsDockerAvailable { get; private set; } = true;

    public QdrantClient Client => this._client
        ?? throw new InvalidOperationException("No Qdrant container is running - check IsDockerAvailable first.");

    public async ValueTask InitializeAsync()
    {
        try
        {
            this._container = new QdrantBuilder(Image).Build();
            await this._container.StartAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Docker is not reachable from this machine right now - skip this tier rather than fail the build.
            this.IsDockerAvailable = false;
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
}