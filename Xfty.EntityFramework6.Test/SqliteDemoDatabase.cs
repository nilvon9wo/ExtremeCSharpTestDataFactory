using System.Data.SQLite;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.EntityFramework6.Test;

/// <summary>
/// A throwaway, file-backed SQLite database with <see cref="DemoDbContext"/>'s
/// schema, for one test class instance. File-backed rather than EF Core's
/// `:memory:` trick, and with its schema created by hand, because
/// `System.Data.SQLite.EF6`'s provider doesn't implement
/// `DbProviderServices.DbCreateDatabaseScript` at all (confirmed by running
/// it, not assumed - a real, longstanding gap in that community provider, not
/// a bug here), so `Database.Create()`/`CreateIfNotExists()`/
/// `ObjectContext.CreateDatabaseScript()` all throw or silently create
/// nothing against SQLite specifically. The tables below mirror exactly the
/// columns <see cref="DemoDbContext.OnModelCreating"/> maps (everything on
/// <see cref="Account"/>/<see cref="Contact"/> except the reflection-only
/// navigation properties both already `Ignore(...)` out, plus
/// <see cref="Ticket"/>) - SQLite's loose, affinity-based typing means the
/// exact column types barely matter.
/// </summary>
public sealed class SqliteDemoDatabase : IDisposable
{
    private const string CreateAccounts = """
        CREATE TABLE Accounts (
            Id TEXT PRIMARY KEY, Name TEXT, Industry TEXT, Type TEXT,
            NumberOfEmployees INTEGER, AnnualRevenue REAL, Site TEXT, Description TEXT,
            OwnerId TEXT, ParentId TEXT, AccountNumber TEXT,
            ShippingStreet TEXT, ShippingCity TEXT, ShippingCountry TEXT,
            BillingCity TEXT, BillingStreet TEXT
        )
        """;

    private const string CreateContacts = """
        CREATE TABLE Contacts (
            Id TEXT PRIMARY KEY, FirstName TEXT, LastName TEXT, Email TEXT,
            AccountId TEXT, ReportsToId TEXT, Department TEXT, Birthdate TEXT
        )
        """;

    private const string CreateTickets = """
        CREATE TABLE Tickets (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Subject TEXT
        )
        """;

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"xfty-ef6-{Guid.NewGuid():N}.sqlite");
    private readonly SQLiteConnection _connection;

    public SqliteDemoDatabase()
    {
        this._connection = new SQLiteConnection($"Data Source={this._dbPath};Version=3;");
        this._connection.Open();
        this.Context = new DemoDbContext(this._connection, contextOwnsConnection: false);
        this.Execute(CreateAccounts);
        this.Execute(CreateContacts);
        this.Execute(CreateTickets);
    }

    public DemoDbContext Context { get; }

    public void Dispose()
    {
        this.Context.Dispose();
        this._connection.Dispose();
        // System.Data.SQLite pools native connections by default; ClearAllPools() forces
        // an immediate release, but even that isn't always synchronous with the delete
        // below - best-effort cleanup of a temp file, not part of what any test proves.
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

    private void Execute(string sql)
    {
        using SQLiteCommand command = new(sql, this._connection);
        _ = command.ExecuteNonQuery();
    }
}