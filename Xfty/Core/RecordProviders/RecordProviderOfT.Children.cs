using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.Children;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>RecordProvider&lt;TRecord&gt; - downward generation (child collections). Each forwards to the identically-named <see cref="RecordProvider"/> method, which carries the documentation.</summary>
public sealed partial class RecordProvider<TRecord>
{
    public RecordProvider<TRecord> With(ChildProvider childProvider) =>
        this.Forwarding(() => this._inner.With(childProvider));

    public RecordProvider<TRecord> WithChildren(PropertyInfo childRelationshipField, int countPerParent) =>
        this.Forwarding(() => this._inner.WithChildren(childRelationshipField, countPerParent));

    public RecordProvider<TRecord> WithChild(PropertyInfo childRelationshipField) =>
        this.Forwarding(() => this._inner.WithChild(childRelationshipField));
}