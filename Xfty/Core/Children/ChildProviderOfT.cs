using System.Linq.Expressions;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;

namespace Net.NowhereAtAll.Xfty.Core.Children;

/// <summary>
/// The ergonomic, strongly-typed way to build a <see cref="ChildProvider"/> -
/// a thin wrapper, not a second implementation, matching
/// <see cref="RecordProvider{TRecord}"/>'s own pattern. Names the
/// relationship field by lambda instead of <see cref="Field.Of{TRecord}(Expression{Func{TRecord,object}})"/>,
/// and every fluent method returns this wrapper so the whole chain stays
/// typed:
///
/// <code>
/// .With(new ChildProvider&lt;Case&gt;(x => x.AccountId) { [x => x.Subject] = "Escalated" })
/// </code>
///
/// Converts implicitly to the plain <see cref="ChildProvider"/> everything
/// else (<see cref="RecordProvider.With(ChildProvider)"/> included) already
/// works with. The forwarders are split across files by the same concern as
/// <see cref="RecordProvider{TRecord}"/>'s partials: FieldConfig, Setters.
/// </summary>
public sealed partial class ChildProvider<TChild>
{
    private readonly ChildProvider _inner;

    public ChildProvider(Expression<Func<TChild, object?>> relationshipField) =>
        this._inner = new ChildProvider(Field.Of(relationshipField));

    public ChildProvider(Expression<Func<TChild, object?>> relationshipField, TChild template) =>
        this._inner = new ChildProvider(Field.Of(relationshipField), template);

    /// <summary>Object-initializer field configuration, mirroring <see cref="RecordProvider{TRecord}"/>'s own indexer.</summary>
    public object? this[Expression<Func<TChild, object?>> field]
    {
        set => _ = this._inner.Put(Field.Of(field), value);
    }

    public static implicit operator ChildProvider(ChildProvider<TChild> typed) => typed._inner;

    public PropertyInfo RelationshipField => this._inner.RelationshipField;

    public Type ChildType => this._inner.ChildType;

    /// <summary>
    /// Runs one configuration call against <see cref="_inner"/> and returns this
    /// wrapper - never the <see cref="ChildProvider"/> the inner call hands back -
    /// so the fluent chain stays typed as <see cref="ChildProvider{TChild}"/>.
    /// Every fluent forwarder in the other partials is one of these.
    /// </summary>
    private ChildProvider<TChild> Forwarding(Func<ChildProvider> innerCall)
    {
        _ = innerCall();
        return this;
    }
}