namespace Net.Nowhereatall.Xfty.Core.Values;

/// <summary>
/// A value that needs no context to produce - a literal, a counter, a random
/// unique token. See <c>IContextAwareExpression</c> (not yet ported - depends
/// on the not-yet-ported generation-context type) for values that read
/// sibling fields or an ancestor record.
/// </summary>
public interface IValueExpression
{
    object? Get();
}
