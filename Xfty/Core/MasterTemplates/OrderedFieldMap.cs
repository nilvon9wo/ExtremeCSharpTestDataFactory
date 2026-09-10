using System.Collections;
using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Core.MasterTemplates;

/// <summary>
/// A field-keyed map that remembers the order fields were first added. The
/// value passes over a <see cref="MasterTemplate"/> need a deterministic order
/// - a context-aware value may read an earlier one - which a plain dictionary
/// does not give.
///
/// Re-adding a field replaces its value but keeps its position; removing a
/// field forgets its position, so a later re-add lands at the end.
/// </summary>
internal sealed class OrderedFieldMap<TValue> : IEnumerable<KeyValuePair<PropertyInfo, TValue>>
{
    private readonly List<PropertyInfo> _order = [];
    private readonly Dictionary<PropertyInfo, TValue> _byField = [];

    public int Count => this._byField.Count;

    /// <summary>The fields, in the order they were first added.</summary>
    public IReadOnlyList<PropertyInfo> Keys => this._order;

    public TValue this[PropertyInfo field] => this._byField[field];

    public bool ContainsKey(PropertyInfo field) => this._byField.ContainsKey(field);

    /// <summary>Add field, or replace its value in place when it is already present.</summary>
    public void Set(PropertyInfo field, TValue value)
    {
        if (!this._byField.ContainsKey(field))
        {
            this._order.Add(field);
        }

        this._byField[field] = value;
    }

    public bool Remove(PropertyInfo field)
    {
        _ = this._order.Remove(field);
        return this._byField.Remove(field);
    }

    public IEnumerator<KeyValuePair<PropertyInfo, TValue>> GetEnumerator() =>
        this._order
            .Select(key => new KeyValuePair<PropertyInfo, TValue>(key, this._byField[key]))
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}