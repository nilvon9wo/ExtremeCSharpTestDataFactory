using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Core;
using Pgvector.EntityFrameworkCore;
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
    private PostgreSqlContainer? container;
    private PgVectorDbContext? dbContext;
    private bool dockerAvailable = true;

    public async ValueTask InitializeAsync()
    {
        try
        {
            this.container = new PostgreSqlBuilder("pgvector/pgvector:pg16").Build();
            await this.container.StartAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Docker is not reachable from this machine right now - skip this tier rather than fail the build.
            this.dockerAvailable = false;
            return;
        }

        DbContextOptions<PgVectorDbContext> options = new DbContextOptionsBuilder<PgVectorDbContext>()
            .UseNpgsql(this.container.GetConnectionString(), o => o.UseVector())
            .Options;
        this.dbContext = new PgVectorDbContext(options);
        _ = await this.dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.dbContext is not null)
        {
            await this.dbContext.DisposeAsync().ConfigureAwait(false);
        }

        if (this.container is not null)
        {
            await this.container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task Supply_InNowMode_AgainstARealPgvectorColumn_ActuallyInsertsTheVector()
    {
        Assert.SkipUnless(this.dockerAvailable, "Docker is not reachable from this machine - start Docker Desktop to run this tier.");

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(DocumentEmbedding), new DocumentEmbeddingProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this.dbContext!));

        // Act
        DocumentEmbedding result = (DocumentEmbedding)await provider.Supply();

        // Assert
        Assert.NotNull(result.Id);
        DocumentEmbedding reread = this.dbContext!.DocumentEmbeddings.AsNoTracking().First(x => x.Id == result.Id);
        Assert.NotNull(reread.Embedding);
        Assert.Equal(DocumentEmbedding.EmbeddingDimensions, reread.Embedding!.ToArray().Length);
    }
}
