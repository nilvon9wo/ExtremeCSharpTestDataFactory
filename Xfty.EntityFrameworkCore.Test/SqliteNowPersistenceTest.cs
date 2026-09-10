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
    private readonly SqliteConnection Connection;
    private readonly DemoDbContext DbContext;

    public SqliteNowPersistenceTest()
    {
        // an in-memory SQLite database needs one open connection kept alive for its lifetime
        this.Connection = new SqliteConnection("DataSource=:memory:");
        this.Connection.Open();
        this.DbContext = new DemoDbContext(new DbContextOptionsBuilder<DemoDbContext>().UseSqlite(this.Connection).Options);
        _ = this.DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        this.DbContext.Dispose();
        this.Connection.Dispose();
    }

    [Fact]
    public async Task Supply_InNowMode_ActuallyInsertsARowIntoTheDatabase()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this.DbContext));

        // Act
        Account result = (Account)await provider.Supply().ConfigureAwait(true);

        // Assert - not just an in-memory Id: a real row is there for a fresh query to find
        Assert.NotNull(result.Id);
        Account? reread = this.DbContext.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == result.Id);
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
            .SetPersistenceGateway(new EfPersistenceGateway(this.DbContext));

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact contact = (Contact)bundle.PrimaryRecords()![0];
        Account account = (Account)bundle.GetList<Contact>(x => x.AccountId)![0];
        Assert.Equal(1, this.DbContext.Accounts.Count());
        Assert.Equal(1, this.DbContext.Contacts.Count());
        Contact? rereadContact = this.DbContext.Contacts.AsNoTracking().First();
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
            .SetPersistenceGateway(new EfPersistenceGateway(this.DbContext))
            .DepthBatched();

        // Act
        Contact result = (Contact)await provider.Supply().ConfigureAwait(true);

        // Assert - both rows are really there, wired to each other, after a depth-batched Now call
        Contact rereadContact = this.DbContext.Contacts.AsNoTracking().First(c => c.Id == result.Id);
        Account rereadAccount = this.DbContext.Accounts.AsNoTracking().First();
        Assert.Equal(rereadAccount.Id, rereadContact.AccountId);
    }

    [Fact]
    public async Task SupplyList_InNowMode_WithQuantity_InsertsEveryRow()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetQuantityPerTemplate(5)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this.DbContext));

        // Act
        List<object> results = await provider.SupplyList().ConfigureAwait(true);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.Equal(5, this.DbContext.Accounts.Count());
    }
}
