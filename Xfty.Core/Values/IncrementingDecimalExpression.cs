namespace Net.Nowhereatall.Xfty.Core.Values;

/// <summary>An <see cref="IValueExpression"/> producing ascending decimals, 1, 2, 3... per instance.</summary>
public sealed class IncrementingDecimalExpression : IValueExpression
{
    private decimal counter = 1;

    public object Get() => this.counter++;
}
