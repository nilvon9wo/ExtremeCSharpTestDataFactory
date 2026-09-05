namespace Net.Nowhereatall.Xfty.Core.Values;

/// <summary>
/// An <see cref="IValueExpression"/> unique within one process's lifetime (the
/// counter is process-static, not per-instance) - not across persisted runs;
/// see <see cref="UniqueAcrossRunsExpression"/> for that.
/// </summary>
public sealed class UniqueStringExpression : IValueExpression
{
    private static int counter = 1;

    private readonly string prefix;

    public UniqueStringExpression(string prefix) => this.prefix = prefix;

    public object Get() => $"{this.prefix} {counter++}";
}
