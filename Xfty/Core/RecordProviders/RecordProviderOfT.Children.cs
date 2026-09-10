using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.Children;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

/// <summary>RecordProvider&lt;TRecord&gt; - downward generation (child collections).</summary>
public sealed partial class RecordProvider<TRecord>
{
    public RecordProvider<TRecord> With(ChildProvider childProvider)
    {
        _ = this._inner.With(childProvider);
        return this;
    }

    public RecordProvider<TRecord> WithChildren(PropertyInfo childRelationshipField, int countPerParent)
    {
        _ = this._inner.WithChildren(childRelationshipField, countPerParent);
        return this;
    }

    public RecordProvider<TRecord> WithChild(PropertyInfo childRelationshipField)
    {
        _ = this._inner.WithChild(childRelationshipField);
        return this;
    }
}