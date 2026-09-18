namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> unique across processes, machines, and
/// persisted runs - not just within one process's lifetime like
/// <see cref="UniqueStringExpression"/> (whose s_counter starts fresh every run).
///
/// The difference matters only when the record is *persisted*: two seed runs
/// each generating "test.username.example1@example.com" collide on the second
/// insert. `prefix` + a per-run token + a s_counter + `suffix`. The token is
/// 16 hex characters (64 bits) sliced from a single <see cref="Guid.NewGuid"/>
/// call, the same collision-resistant primitive a real Guid's own uniqueness
/// rests on - not a hand-rolled mix of a truncated timestamp and a few
/// decimal digits of <c>System.Random</c> output, which is what an earlier
/// version of this class actually used, and which had a real, nonzero
/// collision window: the millisecond count truncated to 9 digits wraps every
/// ~11.6 days, and 5 decimal digits of randomness is under 17 bits of real
/// entropy. A "unique" value that can silently collide is a worse failure
/// mode for a testing library than almost anything else it could get wrong,
/// so this isn't a place to economize on entropy the way a genuinely
/// non-deterministic-by-design value (e.g. <c>Xfty.VectorDatabases</c>'s
/// <c>RandomVectorExpression</c>) legitimately can - that one never claims
/// uniqueness, it only needs values that *look* like independent vector
/// components. Keep `prefix`/`suffix` short - the token adds 16 characters.
/// </summary>
public sealed class UniqueAcrossRunsExpression(string? prefix, string? suffix) : IValueExpression
{
    private static readonly string RunToken = Guid.NewGuid().ToString("N")[..16];
    private static int s_counter = 1;

    private readonly string _prefix = prefix ?? string.Empty;
    private readonly string _suffix = suffix ?? string.Empty;

    public object Get() => $"{this._prefix}{RunToken}{s_counter++}{this._suffix}";
}