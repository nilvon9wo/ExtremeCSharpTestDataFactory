using Net.Nowhereatall.Xfty.Core.Core;
using Net.Nowhereatall.Xfty.Core.Engine;
namespace Net.Nowhereatall.Xfty.Core.Values;

/// <summary>
/// A value expression that needs the surrounding generation context -
/// sibling fields on the record being built, or (once ported) fields on a
/// generated ancestor.
///
/// This is a **separate** interface from <see cref="IValueExpression"/>, not
/// a subtype of it: a context-aware value genuinely cannot produce anything
/// without a context, so making it satisfy the no-argument Get() contract
/// would be a lie. See docs/roadmap/context-aware-values.md.
/// </summary>
public interface IContextAwareExpression
{
    object? Get(GenerationContext context);
}
