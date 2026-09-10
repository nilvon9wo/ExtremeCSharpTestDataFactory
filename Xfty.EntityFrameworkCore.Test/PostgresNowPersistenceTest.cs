using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Testcontainers.PostgreSql;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

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
    private PostgreSqlContainer? _container;
    private DemoDbContext? _dbContext;
    private bool _dockerAvailable = true;

    public async ValueTask InitializeAsync()
    {
        try
        {
            // Build() itself validates Docker connectivity - both it and StartAsync() can be
            // where "Docker is not reachable" surfaces, so both are covered by this one try.
            this._container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await this._container.StartAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Docker is not reachable from this machine right now - skip this tier rather than fail the build.
            this._dockerAvailable = false;
            return;
        }

        DbContextOptions<DemoDbContext> options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseNpgsql(this._container.GetConnectionString())
            .Options;
        this._dbContext = new DemoDbContext(options);
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
    public async Task Supply_InNowMode_AgainstARealPostgresContainer_ActuallyInsertsARow()
    {
        Assert.SkipUnless(
            this._dockerAvailable,
            "Docker is not reachable from this machine - start Docker Desktop to run this tier."
        );

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._dbContext!));

        // Act
        Account result = (Account)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.NotNull(result.Id);
        Account? reread = this._dbContext!.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == result.Id);
        Assert.NotNull(reread);
    }

    [Fact]
    public async Task SupplyBundle_NowPlusDepthBatched_AgainstARealPostgresContainer_WiresTheRealForeignKey()
    {
        Assert.SkipUnless(
            this._dockerAvailable,
            "Docker is not reachable from this machine - start Docker Desktop to run this tier."
        );

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), new DefaultProviderLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._dbContext!))
            .DepthBatched();

        // Act
        Contact result = (Contact)await provider.Supply().ConfigureAwait(true);

        // Assert
        Contact rereadContact = this._dbContext!.Contacts.AsNoTracking().First(c => c.Id == result.Id);
        Account rereadAccount = this._dbContext!.Accounts.AsNoTracking().First();
        Assert.Equal(rereadAccount.Id, rereadContact.AccountId);
    }
}