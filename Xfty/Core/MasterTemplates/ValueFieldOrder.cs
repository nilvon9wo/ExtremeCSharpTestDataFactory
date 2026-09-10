using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Core.MasterTemplates;

/// <summary>
/// The order the value fields (plain + context-aware + deferred) were added to
/// a <see cref="MasterTemplate"/>. A context-aware value may read an earlier
/// one, so the value passes need a deterministic order rather than a dictionary's.
/// </summary>
internal sealed class ValueFieldOrder(IEnumerable<PropertyInfo> initialFields)
{
    private readonly List<PropertyInfo> _order = [.. initialFields];

    public void Append(PropertyInfo field)
    {
        if (!this._order.Contains(field))
        {
            this._order.Add(field);
        }
    }

    public void Remove(PropertyInfo field) =>
        _ = this._order.RemoveAll(each => each == field);

    public List<PropertyInfo> Snapshot() => [.. this._order];
}