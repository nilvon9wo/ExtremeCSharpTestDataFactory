namespace Net.Nowhereatall.Xfty.Demo;

/// <summary>
/// A third demo record type, needed only to exercise a genuine three-level
/// hierarchy (Account -&gt; Contact -&gt; Case) in downward-generation and
/// deep-path tests that Account/Contact alone cannot reach.
/// </summary>
public sealed class Case
{
    public string? Id { get; init; }

    public string? Subject { get; init; }

    public string? Origin { get; init; }

    public string? AccountId { get; init; }

    public string? ContactId { get; init; }
}
