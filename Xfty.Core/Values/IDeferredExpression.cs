using Net.Nowhereatall.Xfty.Core.Core;
using Net.Nowhereatall.Xfty.Core.Engine;
namespace Net.Nowhereatall.Xfty.Core.Values;

/// <summary>
/// A value that reads **up** the graph - a field on a record derived from one
/// of its generated descendants. It cannot be evaluated when the record is
/// built, because the descendant does not exist yet, so it is resolved in a
/// pass over the whole in-memory forest just before the depth-batched insert.
///
/// That forest only exists under the DEFERRED insert mode; a Provider that
/// carries one of these in any other mode throws. The resolved value appears
/// when the deferred flush runs.
/// </summary>
public interface IDeferredExpression
{
    /// <summary>The value for records[recordIndex]'s field, read from its descendants via graph.</summary>
    object? Get(DeferredGraph graph, int recordIndex);
}
