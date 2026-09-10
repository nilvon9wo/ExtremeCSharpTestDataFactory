using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// Proves InsertMode.Now (and .DepthBatched()) against a real database - a
/// SQLite file-backed connection, not a mock. No Docker, no external
/// service; this tier always runs. See PostgresNowPersistenceTest for the
/// Docker-backed tier proving the same thing against a real client-server
/// database.
/// </summary>
public sealed class SqliteNowPersistenceTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DemoDbContext _dbContext;

    public SqliteNowPersistenceTest()
    {
        // an in-memory SQLite database needs one open connection kept alive for its lifetime
        this._connection = new SqliteConnection("DataSource=:memory:");
        this._connection.Open();
        this._dbContext = new DemoDbContext(new DbContextOptionsBuilder<DemoDbContext>().UseSqlite(this._connection).Options);
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

    [Fact]
    public async Task SupplyBundle_InNowMode_InsertsTheRequiredParentRowToo()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), new DefaultProviderLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._dbContext));

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Equal(1, this._dbContext.Accounts.Count());
        Assert.Equal(1, this._dbContext.Contacts.Count());
        Contact? rereadContact = this._dbContext.Contacts.AsNoTracking().First();
        Assert.Equal(account.Id, rereadContact.AccountId);
        Assert.Equal(contact.Id, rereadContact.Id);
    }

    [Fact]
    public async Task Supply_NowPlusDepthBatched_InsertsOneSaveChangesCallPerDependencyLayer()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), new DefaultProviderLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._dbContext))
            .DepthBatched();

        // Act
        Contact result = (Contact)await provider.Supply().ConfigureAwait(true);

        // Assert - both rows are really there, wired to each other, after a depth-batched Now call
        Contact rereadContact = this._dbContext.Contacts.AsNoTracking().First(c => c.Id == result.Id);
        Account rereadAccount = this._dbContext.Accounts.AsNoTracking().First();
        Assert.Equal(rereadAccount.Id, rereadContact.AccountId);
    }

    [Fact]
    public async Task SupplyList_InNowMode_WithQuantity_InsertsEveryRow()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetQuantityPerTemplate(5)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._dbContext));

        // Act
        List<object> results = await provider.SupplyList().ConfigureAwait(true);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.Equal(5, this._dbContext.Accounts.Count());
    }
}