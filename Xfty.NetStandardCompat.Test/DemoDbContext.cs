using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.NetStandardCompat.Test;

/// <summary>
/// A trimmed copy of Xfty.EntityFrameworkCore.Test's own DemoDbContext - just
/// enough of <see cref="Account"/>'s mapping to prove
/// <see cref="Net.NowhereAtAll.Xfty.EntityFrameworkCore.EfPersistenceGateway"/> against a real
/// database on net472/EF Core 3.1.x. Not a ProjectReference to that test
/// project: it targets net10.0 and pulls in Postgres/Testcontainers
/// dependencies with no netstandard2.0/net472 story of their own - nothing
/// this smoke test needs. See <see cref="SqliteNowPersistenceSmokeTest"/>.
/// </summary>
public sealed class DemoDbContext(DbContextOptions<DemoDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => this.Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Account>(account =>
        {
            _ = account.HasKey(x => x.Id);
            _ = account.Ignore(x => x.Contacts);
            _ = account.Ignore(x => x.Parent);
            _ = account.Ignore(x => x.ChildAccounts);
        });
}