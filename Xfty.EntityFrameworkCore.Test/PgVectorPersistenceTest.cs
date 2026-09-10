using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Testcontainers.PostgreSql;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// Proves the cheap pgvector option from docs/roadmap/vector-databases.md:
/// a vector column persists for real through the *existing, unmodified*
/// <see cref="EfPersistenceGateway"/> - no new gateway code, just a
/// Pgvector.EntityFrameworkCore reference and an entity shape. Uses the
/// <c>pgvector/pgvector:pg16</c> image (not the plain <c>postgres:16-alpine</c>
/// image the rest of this project uses) because the vector extension has to
/// actually be compiled into the Postgres image to be creatable at all.
/// Requires a running Docker daemon; skips (rather than fails) when one
/// isn't reachable, same as <see cref="PostgresNowPersistenceTest"/>.
/// </summary>
[Trait("Category", "Docker")]
public sealed class PgVectorPersistenceTest : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private PgVectorDbContext? _dbContext;
    private bool _dockerAvailable = true;

    public async ValueTask InitializeAsync()
    {
        try
        {
            this._container = new PostgreSqlBuilder("pgvector/pgvector:pg16").Build();
            await this._container.StartAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Docker is not reachable from this machine right now - skip this tier rather than fail the build.
            this._dockerAvailable = false;
            return;
        }

        DbContextOptions<PgVectorDbContext> options = new DbContextOptionsBuilder<PgVectorDbContext>()
            .UseNpgsql(this._container.GetConnectionString(), o => o.UseVector())
            .Options;
        this._dbContext = new PgVectorDbContext(options);
        _ = await this._dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (this._dbContext is not null)
        {
            await this._dbContext.DisposeAsync().ConfigureAwait(false);
        }

        if (this._container is not null)
        {
            await this._container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task Supply_InNowMode_AgainstARealPgvectorColumn_ActuallyInsertsTheVector()
    {
        Assert.SkipUnless(
            this._dockerAvailable,
            "Docker is not reachable from this machine - start Docker Desktop to run this tier."
        );

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(DocumentEmbedding), new DocumentEmbeddingProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._dbContext!));

        // Act
        DocumentEmbedding result = (DocumentEmbedding)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.NotNull(result.Id);
        DocumentEmbedding reread = this._dbContext!.DocumentEmbeddings.AsNoTracking().First(x => x.Id == result.Id);
        Assert.NotNull(reread.Embedding);
        Assert.Equal(DocumentEmbedding.EmbeddingDimensions, reread.Embedding!.ToArray().Length);
    }
}