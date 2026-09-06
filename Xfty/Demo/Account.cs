namespace Net.NowhereAtAll.Xfty.Demo;

/// <summary>
/// A minimal demo record type used across this library's own examples and
/// tests - the Contact/Account pair keeps those clean and recognisable. Grows
/// as needed; this is not meant to be exhaustive.
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

    public string? ParentId { get; init; }

    public string? AccountNumber { get; init; }

    public string? ShippingStreet { get; init; }

    public string? ShippingCity { get; init; }

    public string? ShippingCountry { get; init; }

    public string? BillingCity { get; init; }

    public string? BillingStreet { get; init; }

    /// <summary>
    /// The Contacts child collection - populated only via reflection, by
    /// <see cref="Enrichment.BundleEnricher"/>, since a plain instance has no
    /// implicit relationship support and needs somewhere to graft one.
    /// </summary>
    public List<Contact>? Contacts { get; init; }

    /// <summary>The self-referencing Parent ancestor - populated only via reflection, see <see cref="Contacts"/>.</summary>
    public Account? Parent { get; init; }

    /// <summary>The self-referencing ChildAccounts collection (the inverse of ParentId) - populated only via reflection, see <see cref="Contacts"/>.</summary>
    public List<Account>? ChildAccounts { get; init; }
}
