namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore.Test;

/// <summary>
/// An entity with a database-generated <c>int</c> key - the one key shape the
/// demo domain (all <c>string</c> Ids) lacks, so the gateway's "not a string
/// key, leave it to the database" path has something real to run against.
/// </summary>
public sealed class Ticket
{
    public int Id { get; set; }

    public string? Subject { get; set; }
}