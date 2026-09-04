namespace Net.Nowhereatall.Xfty.Core.Demo;

/// <summary>
/// A minimal stand-in for the Salesforce standard Account object, used across
/// the port's examples and tests as the demo record type - the Contact/Account
/// pair chosen so before/after comparisons against the Apex original (and
/// against AutoFixture examples) stay clean. Grows as later ported modules
/// need more fields; this is not meant to be exhaustive.
/// </summary>
public sealed class Account
{
    public string? Name { get; set; }

    public string? Industry { get; set; }

    public string? Type { get; set; }

    public int? NumberOfEmployees { get; set; }

    public decimal? AnnualRevenue { get; set; }
}
