namespace Net.Nowhereatall.Xfty.Core.Values;

/// <summary>
/// An <see cref="IValueExpression"/> producing "prefix 1", "prefix 2", ... per
/// instance - or "prefix1", "prefix2" with <see cref="DontSeparatePrefix"/>.
/// </summary>
public sealed class IncrementingStringExpression : IValueExpression
{
    public const bool SeparatePrefix = true;
    public const bool DontSeparatePrefix = false;

    private readonly string prefix;
    private readonly bool separatePrefix;

    private int counter = 1;

    public IncrementingStringExpression(string prefix, bool separatePrefix = SeparatePrefix)
    {
        this.prefix = prefix;
        this.separatePrefix = separatePrefix;
    }

    public object Get()
    {
        string separator = this.separatePrefix
            ? " "
            : "";
        return this.prefix + separator + this.counter++;
    }
}
