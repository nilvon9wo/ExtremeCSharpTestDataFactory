using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.EntityFramework6;

namespace Net.NowhereAtAll.Xfty.NetStandardCompat.Test;

/// <summary>
/// EF6's code-based provider registration, duplicated from
/// Xfty.EntityFramework6.Test's own SqliteEf6Configuration - see that file's
/// docstring. A second, small copy rather than a shared one: this project
/// can't reference a Test project (no ProjectReference cycle risk, but no
/// real reuse benefit either for four lines of setup).
/// </summary>
internal sealed class Ef6SqliteConfiguration : DbConfiguration
{
    public Ef6SqliteConfiguration()
    {
        this.SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
        this.SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
        this.SetProviderServices(
            "System.Data.SQLite",
            (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)));
    }
}

/// <summary>
/// A trimmed EF6 mapping of just <see cref="Account"/> - enough to prove
/// <see cref="Ef6PersistenceGateway"/>, nothing more. See
/// Xfty.EntityFramework6.Test's own (fuller, Account+Contact) DemoDbContext
/// for the primary suite this smoke test doesn't need to re-prove.
/// </summary>
[DbConfigurationType(typeof(Ef6SqliteConfiguration))]
internal sealed class Ef6DemoDbContext(System.Data.Common.DbConnection connection, bool contextOwnsConnection)
    : DbContext(connection, contextOwnsConnection)
{
    public DbSet<Account> Accounts => this.Set<Account>();

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<Account>().HasKey(x => x.Id);
        _ = modelBuilder.Entity<Account>().Ignore(x => x.Contacts);
        _ = modelBuilder.Entity<Account>().Ignore(x => x.Parent);
        _ = modelBuilder.Entity<Account>().Ignore(x => x.ChildAccounts);
    }
}

/// <summary>
/// Proves Xfty.EntityFramework6's `net461` asset - a distinct physical
/// build from its `netstandard2.1` one, not a netstandard2.0 build at all
/// (see this project's own csproj comment, reason 3) - actually persists a
/// row for real through classic EF6 on a genuine .NET Framework runtime,
/// not just compiles against that old an API (this project targets net472,
/// one Framework generation above net461, so it resolves and runs that
/// same net461 asset). A file-backed SQLite database, and
/// hand-written schema, for the same reasons documented on
/// Xfty.EntityFramework6.Test's own SqliteNowPersistenceTest - this is one
/// trimmed scenario from that suite, not a re-run of the whole thing.
/// </summary>
public sealed class Ef6SmokeTest : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"xfty-ef6-net472-{Guid.NewGuid():N}.sqlite");
    private readonly SQLiteConnection _connection;
    private readonly Ef6DemoDbContext _dbContext;

    public Ef6SmokeTest()
    {
        this._connection = new SQLiteConnection($"Data Source={this._dbPath};Version=3;");
        this._connection.Open();
        this._dbContext = new Ef6DemoDbContext(this._connection, contextOwnsConnection: false);

        const string createAccounts = """
            CREATE TABLE Accounts (
                Id TEXT PRIMARY KEY, Name TEXT, Industry TEXT, Type TEXT,
                NumberOfEmployees INTEGER, AnnualRevenue REAL, Site TEXT, Description TEXT,
                OwnerId TEXT, ParentId TEXT, AccountNumber TEXT,
                ShippingStreet TEXT, ShippingCity TEXT, ShippingCountry TEXT,
                BillingCity TEXT, BillingStreet TEXT
            )
            """;
        using SQLiteCommand createCommand = new(createAccounts, this._connection);
        _ = createCommand.ExecuteNonQuery();
    }

    public void Dispose()
    {
        this._dbContext.Dispose();
        this._connection.Dispose();
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
}