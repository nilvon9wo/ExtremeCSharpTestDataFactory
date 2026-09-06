namespace Net.NowhereAtAll.Xfty.Demo;

/// <summary>
/// A minimal demo record type - just enough (a self-referencing ManagerId) to
/// exercise deep/hierarchical relationship paths and multi-variant Provider
/// chains in tests. Deliberately does not carry a Profile, Username, Role, or
/// anything else that would need a live directory service to resolve.
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? ManagerId { get; init; }
}
