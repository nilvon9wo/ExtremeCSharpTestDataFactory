namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>An <see cref="IValueYielding"/> that always returns the same fixed value, null included.</summary>
public sealed class LiteralExpression(object? value) : IValueYielding
{
    private readonly object? _value = value;

    public object? Get() => this._value;
}