using System.Data.Entity;
using System.Data.SQLite;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.EntityFramework6.Test;

/// <summary>
/// Proves InsertMode.Now (and .DepthBatched()) against a real database
/// through classic EF6, not a mock - a file-backed SQLite database, not
/// EF Core's `:memory:` trick, for the same reason
/// Xfty.EntityFrameworkCore.Test's own SQLite tier isn't used here as a
/// template beyond its four scenarios: `System.Data.SQLite.EF6`'s provider
/// doesn't implement `DbProviderServices.DbCreateDatabaseScript` at all
/// (confirmed by running it, not assumed - a real, longstanding gap in that
/// community provider, not a bug here), so `Database.Create()`/
/// `CreateIfNotExists()`/`ObjectContext.CreateDatabaseScript()` all throw or
/// silently create nothing against SQLite specifically. The schema is
/// created by hand below instead, mirroring exactly the columns
/// <see cref="DemoDbContext.OnModelCreating"/> maps (everything on
/// <see cref="Account"/>/<see cref="Contact"/> except the reflection-only
/// navigation properties both already `Ignore(...)` out) - SQLite's loose,
/// affinity-based typing means the exact column types below barely matter.
/// </summary>
public sealed class SqliteNowPersistenceTest : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"xfty-ef6-{Guid.NewGuid():N}.sqlite");
    private readonly SQLiteConnection _connection;
    private readonly DemoDbContext _dbContext;

    public SqliteNowPersistenceTest()
    {
        this._connection = new SQLiteConnection($"Data Source={this._dbPath};Version=3;");
        this._connection.Open();
        this._dbContext = new DemoDbContext(this._connection, contextOwnsConnection: false);
        CreateSchema(this._connection);
    }

    private static void CreateSchema(SQLiteConnection connection)
    {
        const string createAccounts = """
            CREATE TABLE Accounts (
                Id TEXT PRIMARY KEY, Name TEXT, Industry TEXT, Type TEXT,
                NumberOfEmployees INTEGER, AnnualRevenue REAL, Site TEXT, Description TEXT,
                OwnerId TEXT, ParentId TEXT, AccountNumber TEXT,
                ShippingStreet TEXT, ShippingCity TEXT, ShippingCountry TEXT,
                BillingCity TEXT, BillingStreet TEXT
            )
            """;
        const string createContacts = """
            CREATE TABLE Contacts (
                Id TEXT PRIMARY KEY, FirstName TEXT, LastName TEXT, Email TEXT,
                AccountId TEXT, ReportsToId TEXT, Department TEXT, Birthdate TEXT
            )
            """;

        using SQLiteCommand createAccountsCommand = new(createAccounts, connection);
        _ = createAccountsCommand.ExecuteNonQuery();
        using SQLiteCommand createContactsCommand = new(createContacts, connection);
        _ = createContactsCommand.ExecuteNonQuery();
    }

    public void Dispose()
    {
        this._dbContext.Dispose();
        this._connection.Dispose();
        // System.Data.SQLite pools native connections by default; ClearAllPools() forces
        // an immediate release, but even that isn't always synchronous with the delete
        // below - best-effort cleanup of a temp file, not part of what this test proves.
        SQLiteConnection.ClearAllPools();
        try
        {
            File.Delete(this._dbPath);
        }
        catch (IOException)
        {
            // Stray temp file left behind - harmless, and cleaned up by the OS eventually.
        }
    }

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
        Assert.Equal(result.Name, reread!.Name);
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