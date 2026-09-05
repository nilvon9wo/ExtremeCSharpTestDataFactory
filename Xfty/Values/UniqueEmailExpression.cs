namespace Net.Nowhereatall.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> producing well-formed, unique-within-one-
/// process "prefix1@example.com" style addresses.
/// </summary>
public sealed class UniqueEmailExpression : IValueExpression
{
    private static int _counter = 1;

    private readonly string prefix;

    public UniqueEmailExpression(string prefix) => this.prefix = prefix;

    public object Get() => $"{this.prefix}{_counter++}@example.com";
}
