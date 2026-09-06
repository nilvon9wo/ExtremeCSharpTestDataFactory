using Microsoft.EntityFrameworkCore;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// A dedicated DbContext for the pgvector proof - kept separate from
/// <see cref="DemoDbContext"/> (shared by the SQLite tier, which has no
/// vector extension) so this stays isolated to the Postgres-only,
/// pgvector-image-only tier that needs it.
/// </summary>
public sealed class PgVectorDbContext(DbContextOptions<PgVectorDbContext> options) : DbContext(options)
{
    public DbSet<DocumentEmbedding> DocumentEmbeddings => this.Set<DocumentEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.HasPostgresExtension("vector");
        _ = modelBuilder.Entity<DocumentEmbedding>(entity => entity.HasKey(x => x.Id));
    }
}
