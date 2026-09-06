namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> producing well-formed, unique-within-one-
/// process "prefix1@example.com" style addresses.
/// </summary>
public sealed class UniqueEmailExpression(string prefix) : IValueExpression
{
    private static int _counter = 1;

    private readonly string prefix = prefix;

    public object Get() => $"{this.prefix}{_counter++}@example.com";
}
