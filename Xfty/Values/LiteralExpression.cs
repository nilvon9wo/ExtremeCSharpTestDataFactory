namespace Net.Nowhereatall.Xfty.Values;

/// <summary>An <see cref="IValueExpression"/> that always returns the same fixed value, null included.</summary>
public sealed class LiteralExpression : IValueExpression
{
    private readonly object? value;

    public LiteralExpression(object? value) => this.value = value;

    public object? Get() => this.value;
}
