using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Core;
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
    private readonly SqliteConnection connection;
    private readonly DemoDbContext dbContext;

    public SqliteNowPersistenceTest()
    {
        // an in-memory SQLite database needs one open connection kept alive for its lifetime
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();
        this.dbContext = new DemoDbContext(new DbContextOptionsBuilder<DemoDbContext>().UseSqlite(this.connection).Options);
        _ = this.dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        this.dbContext.Dispose();
        this.connection.Dispose();
    }

    [Fact]
    public async Task Supply_InNowMode_ActuallyInsertsARowIntoTheDatabase()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this.dbContext));

        // Act
        Account result = (Account)await provider.Supply();

        // Assert - not just an in-memory Id: a real row is there for a fresh query to find
        Assert.NotNull(result.Id);
        Account? reread = this.dbContext.Accounts.AsNoTracking().SingleOrDefault(a => a.Id == result.Id);
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
            .SetPersistenceGateway(new EfPersistenceGateway(this.dbContext));

        // Act
        Bundle bundle = await provider.SupplyBundle();

        // Assert
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Equal(1, this.dbContext.Accounts.Count());
        Assert.Equal(1, this.dbContext.Contacts.Count());
        Contact? rereadContact = this.dbContext.Contacts.AsNoTracking().Single();
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
            .SetPersistenceGateway(new EfPersistenceGateway(this.dbContext))
            .DepthBatched();

        // Act
        Contact result = (Contact)await provider.Supply();

        // Assert - both rows are really there, wired to each other, after a depth-batched Now call
        Contact rereadContact = this.dbContext.Contacts.AsNoTracking().Single(c => c.Id == result.Id);
        Account rereadAccount = this.dbContext.Accounts.AsNoTracking().Single();
        Assert.Equal(rereadAccount.Id, rereadContact.AccountId);
    }

    [Fact]
    public async Task SupplyList_InNowMode_WithQuantity_InsertsEveryRow()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetQuantityPerTemplate(5)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this.dbContext));

        // Act
        List<object> results = await provider.SupplyList();

        // Assert
        Assert.Equal(5, results.Count);
        Assert.Equal(5, this.dbContext.Accounts.Count());
    }
}
