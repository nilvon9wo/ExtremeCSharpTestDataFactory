namespace Net.Nowhereatall.Xfty.Demo;

/// <summary>
/// A minimal stand-in for the Salesforce standard Account object, used across
/// the port's examples and tests as the demo record type - the Contact/Account
/// pair chosen so before/after comparisons against the Apex original (and
/// against AutoFixture examples) stay clean. Grows as later ported modules
/// need more fields; this is not meant to be exhaustive.
///
/// A plain mutable class with <c>init</c>-only properties - reflection-based
/// field access (<see cref="Field"/>, the predicates) only ever reads (or, for
/// <see cref="Persistence.IdMocker"/>, writes via reflection, which bypasses
/// the compile-time-only <c>init</c> restriction), so it works identically
/// here and against <see cref="Contact"/>'s record shape.
/// </summary>
public sealed class Account
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? Industry { get; init; }

    public string? Type { get; init; }

    public int? NumberOfEmployees { get; init; }

    public decimal? AnnualRevenue { get; init; }

    public string? Site { get; init; }

    public string? Description { get; init; }

    public string? OwnerId { get; init; }

    public string? AccountNumber { get; init; }

    public string? ShippingStreet { get; init; }

    public string? ShippingCity { get; init; }

    public string? ShippingCountry { get; init; }

    public string? BillingCity { get; init; }

    public string? BillingStreet { get; init; }

    /// <summary>
    /// The Contacts child collection - populated only via reflection, by
    /// <see cref="Enrichment.BundleEnricher"/> (Salesforce SObjects always
    /// support a queried subquery here without declaring it; a plain C#
    /// record needs somewhere to graft one).
    /// </summary>
    public List<Contact>? Contacts { get; init; }
}
