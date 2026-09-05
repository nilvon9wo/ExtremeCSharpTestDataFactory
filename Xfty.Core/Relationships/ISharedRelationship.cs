namespace Net.Nowhereatall.Xfty.Core.Relationships;

/// <summary>
/// A relationship whose parent record is **shared** - every child that
/// references it gets the same record (and the same Id), and it is generated
/// at most once per test method.
///
/// The factory branches on this interface: instead of generating one parent
/// per child it resolves the shared record once and points every child at it.
/// </summary>
public interface ISharedRelationship : IDefaultRelationship
{
    /// <summary>The name this shared record is interned under.</summary>
    string SharedName { get; }

    /// <summary>The one shared record - generated (and cached) on first call, reused after.</summary>
    object? ResolveSharedRecord(GenerationContext context);

    /// <summary>A single-record sub-bundle exposing the shared record. Never null once the record is resolved.</summary>
    Bundle GetResolvedBundle();

    /// <summary>Whether the shared record has a real (inserted) Id - a NOW child needs this to be true.</summary>
    bool IsResolvedRecordPersisted { get; }

    /// <summary>Whether the shared record has been generated yet.</summary>
    bool IsResolved { get; }
}
