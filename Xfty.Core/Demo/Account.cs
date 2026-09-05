namespace Net.Nowhereatall.Xfty.Core.Demo;

/// <summary>
/// A minimal stand-in for the Salesforce standard Account object, used across
/// the port's examples and tests as the demo record type - the Contact/Account
/// pair chosen so before/after comparisons against the Apex original (and
/// against AutoFixture examples) stay clean. Grows as later ported modules
/// need more fields; this is not meant to be exhaustive.
///
/// A plain mutable class with <c>init</c>-only properties - reflection-based
/// field access (<see cref="Field"/>, the predicates) only ever reads, so it
/// works identically here and against <see cref="Contact"/>'s record shape;
/// see that type for the other common C# property-declaration idiom.
/// </summary>
public sealed class Account
{
    public string? Name { get; init; }

    public string? Industry { get; init; }

    public string? Type { get; init; }

    public int? NumberOfEmployees { get; init; }

    public decimal? AnnualRevenue { get; init; }
}
