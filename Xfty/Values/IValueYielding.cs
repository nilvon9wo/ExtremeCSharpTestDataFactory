namespace Net.NowhereAtAll.Xfty.Values;

/// <summary>
/// A value that needs no context to produce - a literal, a counter, a random
/// unique token. See <c>IContextAware</c> (not yet ported - depends
/// on the not-yet-ported generation-context type) for values that read
/// sibling fields or an ancestor record.
/// </summary>
public interface IValueYielding
{
    object? Get();
}