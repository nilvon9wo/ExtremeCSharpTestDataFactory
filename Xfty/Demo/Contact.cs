namespace Net.Nowhereatall.Xfty.Demo;

/// <summary>
/// The other half of the Contact/Account demo pair (see <see cref="Account"/>).
///
/// Declared as a <c>record class</c> rather than a plain class - compiler-
/// generated value equality/<c>ToString</c>, <c>init</c>-only properties.
/// Reflection-based field access reads a record's generated properties
/// exactly like any other property - nothing needs special-casing for it
/// anywhere else in the library.
/// </summary>
public sealed record class Contact
{
    public string? Id { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? AccountId { get; init; }

    public string? ReportsToId { get; init; }

    public string? Department { get; init; }

    public DateTime? Birthdate { get; init; }

    /// <summary>
    /// The Account ancestor - populated only via reflection, by
    /// <see cref="Enrichment.BundleEnricher"/> (see <see cref="Account.Contacts"/>).
    /// </summary>
    public Account? Account { get; init; }

    /// <summary>The Cases child collection - populated only via reflection, see <see cref="Account.Contacts"/>.</summary>
    public List<Case>? Cases { get; init; }
}
