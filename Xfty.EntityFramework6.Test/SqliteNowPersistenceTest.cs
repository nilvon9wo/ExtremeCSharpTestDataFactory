using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.EntityFramework6.Test;

/// <summary>
/// Proves InsertMode.Now (and .DepthBatched()) against a real database
/// through classic EF6, not a mock - see <see cref="SqliteDemoDatabase"/>
/// for why it is a file-backed SQLite database with a hand-made schema.
/// </summary>
public sealed class SqliteNowPersistenceTest : IDisposable
{
    private readonly SqliteDemoDatabase _database = new();
    private readonly DemoDbContext _dbContext;

    public SqliteNowPersistenceTest() => this._dbContext = this._database.Context;

    public void Dispose() => this._database.Dispose();

    [Fact]
    public async Task Supply_InNowMode_ActuallyInsertsARowIntoTheDatabase()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), new DefaultProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new Ef6PersistenceGateway(this._dbContext));

        // Act
        Account result = (Account)await provider.Supply().ConfigureAwait(true);

        // Assert - not just an in-memory Id: a real row is there for a fresh query to find
        Assert.NotNull(result.Id);
        Account? reread = this._dbContext.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == result.Id);
        Assert.NotNull(reread);
        Assert.Equal(result.Name, reread.Name);
    }

    [Fact]
    public async Task SupplyBundle_InNowMode_InsertsTheRequiredParentRowToo()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), new DefaultProviderLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new Ef6PersistenceGateway(this._dbContext));

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
            .SetPersistenceGateway(new Ef6PersistenceGateway(this._dbContext))
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
            .SetPersistenceGateway(new Ef6PersistenceGateway(this._dbContext));

        // Act
        List<object> results = await provider.SupplyList().ConfigureAwait(true);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.Equal(5, this._dbContext.Accounts.Count());
    }
}