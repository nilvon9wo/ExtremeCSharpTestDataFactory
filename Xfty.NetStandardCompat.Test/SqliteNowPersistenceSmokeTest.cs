using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.EntityFrameworkCore;

namespace Net.NowhereAtAll.Xfty.NetStandardCompat.Test;

/// <summary>
/// Proves Xfty.EntityFrameworkCore's netstandard2.0 build - which rides EF
/// Core 3.1.x, not the 9.x/10.x lines the rest of this solution proves on
/// net10.0 (see Xfty.EntityFrameworkCore.csproj's own comment on why) -
/// actually persists a row for real on a genuine down-level runtime, not
/// just compiles against that old a package. A trimmed copy of
/// Xfty.EntityFrameworkCore.Test's SqliteNowPersistenceTest: one scenario,
/// not the whole suite - this only needs to prove the pairing works, not
/// re-prove everything InsertMode.Now already covers on net10.0.
/// </summary>
public sealed class SqliteNowPersistenceSmokeTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DemoDbContext _dbContext;

    public SqliteNowPersistenceSmokeTest()
    {
        // an in-memory SQLite database needs one open connection kept alive for its lifetime
        this._connection = new SqliteConnection("DataSource=:memory:");
        this._connection.Open();
        DbContextOptions<DemoDbContext> options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseSqlite(this._connection)
            .Options;
        this._dbContext = new DemoDbContext(options);
        _ = this._dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        this._dbContext.Dispose();
        this._connection.Dispose();
    }

    [Fact]
    public async Task Supply_InNowMode_ActuallyInsertsARowIntoTheDatabase()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._dbContext));

        // Act
        Account result = (Account)await provider.Supply().ConfigureAwait(true);

        // Assert - not just an in-memory Id: a real row is there for a fresh query to find
        Assert.NotNull(result.Id);
        Account? reread = this._dbContext.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == result.Id);
        Assert.NotNull(reread);
        Assert.Equal(result.Name, reread!.Name);
    }
}