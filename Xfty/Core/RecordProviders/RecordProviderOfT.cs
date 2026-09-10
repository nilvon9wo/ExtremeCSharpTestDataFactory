using System.Linq.Expressions;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>
/// The ergonomic, strongly-typed way to start and run a <see cref="RecordProvider"/>
/// for one record type - a thin wrapper, not a second implementation,
/// mirroring <see cref="MasterTemplate{TRecord}"/>'s own pattern. Every
/// fluent method forwards to the wrapped plain <see cref="RecordProvider"/>
/// and returns this wrapper, so the whole chain - including
/// <see cref="Supply"/>/<see cref="SupplyList"/>'s return type - stays
/// typed as <typeparamref name="TRecord"/> instead of <c>object</c>, with
/// no cast at the call site:
///
/// <code>
/// Contact result = await new RecordProvider&lt;Contact&gt;(lookup)
///     .Put(x => x.FirstName, "Alice")
///     .SetInsertMode(InsertMode.Mock)
///     .Supply();
/// </code>
///
/// Converts implicitly to the plain <see cref="RecordProvider"/> for
/// anything not exposed here directly. <see cref="RecordProvider"/> is
/// sealed, so this is composition (an inner instance), not inheritance -
/// same reason <see cref="MasterTemplate{TRecord}"/> wraps rather than
/// extends <see cref="MasterTemplate"/>.
/// </summary>
public sealed partial class RecordProvider<TRecord>(IProviderLookup providerLookup)
{
    private readonly RecordProvider _inner = new(typeof(TRecord), providerLookup);

    /// <summary>
    /// Object-initializer field configuration, mirroring <see cref="MasterTemplate{TRecord}"/>'s
    /// own indexer: routed by the value's runtime type (an <see cref="IValueExpression"/>,
    /// an <see cref="IContextAwareExpression"/>, an <see cref="IDeferredExpression"/>, or
    /// an exact literal). A relationship throws, naming <c>PutRequired</c>/<c>PutOptional</c>
    /// instead - its requiredness can't be inferred from the value alone.
    /// </summary>
    public object? this[Expression<Func<TRecord, object?>> field]
    {
        set => _ = this._inner.Put(Field.Of(field), value);
    }

    public static implicit operator RecordProvider(RecordProvider<TRecord> typed) => typed._inner;
}