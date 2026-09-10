namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> unique within one process's lifetime (the
/// s_counter is process-static, not per-instance) - not across persisted runs;
/// see <see cref="UniqueAcrossRunsExpression"/> for that.
/// </summary>
public sealed class UniqueStringExpression(string prefix) : IValueExpression
{
    private static int s_counter = 1;

    private readonly string _prefix = prefix;

    public object Get() => $"{this._prefix} {s_counter++}";
}