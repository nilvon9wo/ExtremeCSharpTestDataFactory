using Microsoft.EntityFrameworkCore;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Testcontainers.PostgreSql;

namespace Net.Nowhereatall.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// Proves InsertMode.Now against a real client-server database - a Postgres
/// container started with Docker via Testcontainers - not a mock, and not
/// SQLite's in-process engine. Requires a running Docker daemon; skips
/// (rather than fails) when one isn't reachable, so this tier is opt-in on a
/// developer machine and runs automatically on any CI runner with Docker
/// available (GitHub Actions' ubuntu-latest has it out of the box).
/// </summary>
[Trait("Category", "Docker")]
public sealed class PostgresNowPersistenceTest : IAsyncLifetime
{
    private PostgreSqlContainer? container;
    private DemoDbContext? dbContext;
    private bool dockerAvailable = true;

    public async ValueTask InitializeAsync()
    {
        try
        {
            // Build() itself validates Docker connectivity - both it and StartAsync() can be
            // where "Docker is not reachable" surfaces, so both are covered by this one try.
            this.container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await this.container.StartAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Docker is not reachable from this machine right now - skip this tier rather than fail the build.
            this.dockerAvailable = false;
            return;
        }

        this.dbContext = new DemoDbContext(new DbContextOptionsBuilder<DemoDbContext>().UseNpgsql(this.container.GetConnectionString()).Options);
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
    public void Supply_InNowMode_AgainstARealPostgresContainer_ActuallyInsertsARow()
    {
        Assert.SkipUnless(this.dockerAvailable, "Docker is not reachable from this machine - start Docker Desktop to run this tier.");

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this.dbContext!));

        // Act
        Account result = (Account)provider.Supply();

        // Assert
        Assert.NotNull(result.Id);
        Account? reread = this.dbContext!.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == result.Id);
        Assert.NotNull(reread);
    }

    [Fact]
    public void SupplyBundle_NowPlusDepthBatched_AgainstARealPostgresContainer_WiresTheRealForeignKey()
    {
        Assert.SkipUnless(this.dockerAvailable, "Docker is not reachable from this machine - start Docker Desktop to run this tier.");

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), new DefaultProviderLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this.dbContext!))
            .DepthBatched();

        // Act
        Contact result = (Contact)provider.Supply();

        // Assert
        Contact rereadContact = this.dbContext!.Contacts.AsNoTracking().First(c => c.Id == result.Id);
        Account rereadAccount = this.dbContext!.Accounts.AsNoTracking().First();
        Assert.Equal(rereadAccount.Id, rereadContact.AccountId);
    }
}
