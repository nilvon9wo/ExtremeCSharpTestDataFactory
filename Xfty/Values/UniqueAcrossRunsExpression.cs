using System.Globalization;

namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> unique across processes, machines, and
/// persisted runs - not just within one process's lifetime like
/// <see cref="UniqueStringExpression"/> (whose s_counter starts fresh every run).
///
/// The difference matters only when the record is *persisted*: two seed runs
/// each generating "test.username.example1@example.com" collide on the second
/// insert. `prefix` + a per-run token (time + randomness) + a s_counter +
/// `suffix`. Keep `prefix`/`suffix` short - the token adds ~14 characters.
/// </summary>
public sealed class UniqueAcrossRunsExpression(string? prefix, string? suffix) : IValueExpression
{
    private static readonly string RunToken = BuildRunToken();
    private static int s_counter = 1;

    private readonly string _prefix = prefix ?? string.Empty;
    private readonly string _suffix = suffix ?? string.Empty;

    public object Get() => $"{this._prefix}{RunToken}{s_counter++}{this._suffix}";

    private static string BuildRunToken()
    {
        string nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        string entropy = Math.Abs(SharedRandom.Instance.Next()).ToString(CultureInfo.InvariantCulture);
        return Right(nowMillis, 9) + Right(entropy, 5);
    }

    private static string Right(string text, int length) =>
        text.Length <= length
            ? text
            : text[^length..];
}