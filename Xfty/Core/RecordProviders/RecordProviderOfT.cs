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
/// extends <see cref="MasterTemplate"/>. The forwarders are split across
/// files by the same concern as <see cref="RecordProvider"/>'s own partials:
/// Children, FieldConfig, FieldConfigLambda, Setters, Supply.
/// </summary>
public sealed partial class RecordProvider<TRecord>(IProviderLookup providerLookup)
{
    private readonly RecordProvider inner = new(typeof(TRecord), providerLookup);

    /// <summary>
    /// Object-initializer field configuration, mirroring <see cref="MasterTemplate{TRecord}"/>'s
    /// own indexer: routed by the value's runtime type (an <see cref="IValueExpression"/>,
    /// an <see cref="IContextAwareExpression"/>, an <see cref="IDeferredExpression"/>, or
    /// an exact literal). A relationship throws, naming <c>PutRequired</c>/<c>PutOptional</c>
    /// instead - its requiredness can't be inferred from the value alone.
    /// </summary>
    public object? this[Expression<Func<TRecord, object?>> field]
    {
        set => _ = this.inner.Put(Field.Of(field), value);
    }

    public static implicit operator RecordProvider(RecordProvider<TRecord> typed) => typed.inner;

    /// <summary>
    /// Runs one configuration call against <see cref="inner"/> and returns this
    /// wrapper - never the <see cref="RecordProvider"/> the inner call hands back -
    /// so the fluent chain stays typed as <see cref="RecordProvider{TRecord}"/>.
    /// Every fluent forwarder in the other partials is one of these.
    /// </summary>
    private RecordProvider<TRecord> Forwarding(Func<RecordProvider> innerCall)
    {
        _ = innerCall();
        return this;
    }
}
