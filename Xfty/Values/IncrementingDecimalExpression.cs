namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>An <see cref="IValueYielding"/> producing ascending decimals, 1, 2, 3... per instance.</summary>
public sealed class IncrementingDecimalExpression : IValueYielding
{
    private decimal _counter = 1;

    public object Get() => this._counter++;
}