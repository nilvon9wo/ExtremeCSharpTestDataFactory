using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// The gateway's own key handling, called directly: it fills a missing string
/// key itself (no database generates one for a string column), and leaves every other key
/// alone. The end-to-end InsertMode.Now path is SqliteNowPersistenceTest's.
/// </summary>
public sealed class EfPersistenceGatewayTest : IDisposable
{
    private const string PresetId = "preset-account";

    private readonly SqliteDemoDatabase _database = new();

    public void Dispose() => this._database.Dispose();

    [Fact]
    public async Task Insert_WhenAStringKeyIsAlreadySet_KeepsIt()
    {
        // Arrange
        EfPersistenceGateway gateway = new(this._database.Context);
        Account account = new() { Id = PresetId, Name = "Preset" };

        // Act
        await gateway.Insert([account], Field.Of<Account>(x => x.Id)).ConfigureAwait(true);

        // Assert
        Account reread = this._database.Context.Accounts.AsNoTracking().Single();
        Assert.Equal(PresetId, reread.Id);
    }

    [Fact]
    public async Task Insert_WhenTheKeyIsNotAString_LeavesItForTheDatabaseToGenerate()
    {
        // Arrange
        EfPersistenceGateway gateway = new(this._database.Context);
        Ticket ticket = new() { Subject = "Database-keyed" };

        // Sanity Check
        Assert.Equal(0, ticket.Id);

        // Act
        await gateway.Insert([ticket], Field.Of<Ticket>(x => x.Id)).ConfigureAwait(true);

        // Assert
        Assert.NotEqual(0, ticket.Id);
    }
}