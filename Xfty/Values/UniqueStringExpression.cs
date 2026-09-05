namespace Net.Nowhereatall.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> unique within one process's lifetime (the
/// counter is process-static, not per-instance) - not across persisted runs;
/// see <see cref="UniqueAcrossRunsExpression"/> for that.
/// </summary>
public sealed class UniqueStringExpression(string prefix) : IValueExpression
{
    private static int _counter = 1;

    private readonly string prefix = prefix;

    public object Get() => $"{this.prefix} {_counter++}";
}
