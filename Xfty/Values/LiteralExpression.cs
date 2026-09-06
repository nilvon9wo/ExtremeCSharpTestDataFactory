namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>An <see cref="IValueExpression"/> that always returns the same fixed value, null included.</summary>
public sealed class LiteralExpression(object? value) : IValueExpression
{
    private readonly object? value = value;

    public object? Get() => this.value;
}
