namespace Net.Nowhereatall.Xfty.Demo;

/// <summary>
/// A minimal stand-in for the Salesforce standard User object - just enough
/// (a self-referencing ManagerId) to exercise deep/hierarchical relationship
/// paths and multi-variant Provider chains in tests. Deliberately does not
/// carry Profile/Username/UserRole or anything else that would need a live
/// org - the bundled XFTY_DefaultUserDataProvider itself was not ported for
/// exactly that reason (see csharp-port-idea.md).
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? ManagerId { get; init; }
}
