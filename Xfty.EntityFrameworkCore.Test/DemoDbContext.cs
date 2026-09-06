using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// A minimal real EF Core mapping of this library's own demo domain
/// (<see cref="Account"/>, <see cref="Contact"/>) - just enough schema to
/// prove <see cref="EfPersistenceGateway"/> against an actual database.
/// Navigation-shaped properties that only exist for reflection-based
/// enrichment (<see cref="Account.Contacts"/>, <see cref="Account.Parent"/>,
/// <see cref="Contact.Account"/>, etc.) are ignored - this proves persistence,
/// not a full relational mapping.
/// </summary>
public sealed class DemoDbContext(DbContextOptions<DemoDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => this.Set<Account>();

    public DbSet<Contact> Contacts => this.Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<Account>(account =>
        {
            _ = account.HasKey(x => x.Id);
            _ = account.Ignore(x => x.Contacts);
            _ = account.Ignore(x => x.Parent);
            _ = account.Ignore(x => x.ChildAccounts);
        });
        _ = modelBuilder.Entity<Contact>(contact =>
        {
            _ = contact.HasKey(x => x.Id);
            _ = contact.Ignore(x => x.Account);
            _ = contact.Ignore(x => x.Cases);
        });
    }
}
