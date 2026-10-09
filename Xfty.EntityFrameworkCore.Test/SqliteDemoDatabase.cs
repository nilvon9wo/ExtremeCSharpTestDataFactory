using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// A throwaway in-memory SQLite database with <see cref="DemoDbContext"/>'s
/// schema, for one test class instance. No Docker, no external service.
/// </summary>
public sealed class SqliteDemoDatabase : IDisposable
{
    private const string InMemory = "DataSource=:memory:";

    private readonly SqliteConnection _connection;

    public SqliteDemoDatabase()
    {
        // an in-memory SQLite database needs one open connection kept alive for its lifetime
        this._connection = new SqliteConnection(InMemory);
        this._connection.Open();
        DbContextOptions<DemoDbContext> options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseSqlite(this._connection)
            .Options;
        this.Context = new DemoDbContext(options);
        _ = this.Context.Database.EnsureCreated();
    }

    public DemoDbContext Context { get; }

    public void Dispose()
    {
        this.Context.Dispose();
        this._connection.Dispose();
    }
}