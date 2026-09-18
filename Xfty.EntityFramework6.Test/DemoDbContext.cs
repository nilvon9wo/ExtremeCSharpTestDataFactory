using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.EntityFramework6.Test;

/// <summary>
/// EF6's code-based provider registration - the equivalent of the
/// `&lt;entityFramework&gt;` app.config section, spelled without one (a
/// net10.0 test host doesn't read classic .NET Framework config anyway).
/// <see cref="DbConfigurationType"/> on <see cref="DemoDbContext"/> points at
/// this explicitly rather than relying on EF6's own assembly-scanning
/// auto-discovery, which is unreliable under a modern test host.
/// </summary>
internal sealed class SqliteEf6Configuration : DbConfiguration
{
    public SqliteEf6Configuration()
    {
        this.SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
        this.SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
        this.SetProviderServices(
            "System.Data.SQLite",
            (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)));
    }
}

/// <summary>
/// A trimmed EF6 mapping of this library's own demo domain
/// (<see cref="Account"/>, <see cref="Contact"/>) - just enough schema to
/// prove <see cref="Ef6PersistenceGateway"/> against an actual database.
/// Navigation-shaped properties that only exist for reflection-based
/// enrichment are ignored - this proves persistence, not a full relational
/// mapping. See Xfty.EntityFrameworkCore.Test's own DemoDbContext for the EF
/// Core equivalent this mirrors.
/// </summary>
[DbConfigurationType(typeof(SqliteEf6Configuration))]
public sealed class DemoDbContext(DbConnection connection, bool contextOwnsConnection)
    : DbContext(connection, contextOwnsConnection)
{
    public DbSet<Account> Accounts => this.Set<Account>();

    public DbSet<Contact> Contacts => this.Set<Contact>();

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<Account>().HasKey(x => x.Id);
        _ = modelBuilder.Entity<Account>().Ignore(x => x.Contacts);
        _ = modelBuilder.Entity<Account>().Ignore(x => x.Parent);
        _ = modelBuilder.Entity<Account>().Ignore(x => x.ChildAccounts);

        _ = modelBuilder.Entity<Contact>().HasKey(x => x.Id);
        _ = modelBuilder.Entity<Contact>().Ignore(x => x.Account);
        _ = modelBuilder.Entity<Contact>().Ignore(x => x.Cases);
    }
}