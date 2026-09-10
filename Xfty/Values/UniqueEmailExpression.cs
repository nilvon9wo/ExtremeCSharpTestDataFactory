namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> producing well-formed, unique-within-one-
/// process "prefix1@example.com" style addresses.
/// </summary>
public sealed class UniqueEmailExpression(string prefix) : IValueExpression
{
    private static int s_counter = 1;

    private readonly string _prefix = prefix;

    public object Get() => $"{this._prefix}{s_counter++}@example.com";
}