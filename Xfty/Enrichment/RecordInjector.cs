using System.Collections;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;

namespace Net.NowhereAtAll.Xfty.Enrichment;

/// <summary>
/// Writes onto record instances what an <c>init</c>-only property rejects
/// after construction - a populated parent relationship, a child collection,
/// a forced scalar - via reflection, one clone per row.
///
/// Reflection sets any property directly, regardless of its type, so nothing
/// needs special-casing. Per-record: SetValue bypasses init-only the same way
/// IdMocker and RecordCloneFactory already rely on.
///
/// Standalone: no bundle, no generation. Collect the grafts fluently, then
/// Result() clones every row once and applies them all. Inputs are
/// untouched; the returned list is new instances.
///
/// <code>
/// List&lt;object&gt; withAccount = RecordInjector.Inject(contacts)
///     .Relationship(Field.Of&lt;Contact&gt;(nameof(Contact.Account)), accountsAligned1to1)
///     .Value(Field.Of&lt;Contact&gt;(nameof(Contact.Id)), someId)
///     .Result();
/// </code>
/// </summary>
public sealed class RecordInjector
{
    private readonly List<object> _records;
    private readonly Dictionary<PropertyInfo, List<object>> _parentsByRelationshipField = [];
    private readonly Dictionary<PropertyInfo, List<List<object>>> _childrenByRelationshipField = [];
    private readonly Dictionary<PropertyInfo, object?> _uniformValueByField = [];
    private readonly Dictionary<PropertyInfo, List<object?>> _perRowValuesByField = [];

    private RecordInjector(List<object> records) =>
        this._records = records ?? throw new XftyConfigurationException("RecordInjector needs a records list, not null.");

    public static RecordInjector Inject(List<object> records) => new(records);

    /// <summary>Graft parents[row] onto records[row] under relationshipField (e.g. Contact.Account).</summary>
    public RecordInjector Relationship(PropertyInfo relationshipField, List<object> parents)
    {
        this._parentsByRelationshipField[relationshipField] = parents;
        return this;
    }

    /// <summary>Graft childrenPerRow[row] onto records[row] as relationshipField's collection (e.g. Account.Contacts).</summary>
    public RecordInjector ChildRelationship(PropertyInfo relationshipField, List<List<object>> childrenPerRow)
    {
        this._childrenByRelationshipField[relationshipField] = childrenPerRow;
        return this;
    }

    /// <summary>Set field to the same value on every row.</summary>
    public RecordInjector Value(PropertyInfo field, object? valueForEveryRow)
    {
        this._uniformValueByField[field] = valueForEveryRow;
        return this;
    }

    /// <summary>Set field to values[row] on each row.</summary>
    public RecordInjector ValuePerRow(PropertyInfo field, List<object?> values)
    {
        this._perRowValuesByField[field] = values;
        return this;
    }

    public List<object> Result()
    {
        if (this._records.Count == 0)
        {
            return [];
        }

        this.RejectMisalignedGrafts();
        return [.. this._records.Select(this.GraftedRow)];
    }

    private object GraftedRow(object record, int row)
    {
        object clone = RecordCloneFactory.DeepClone(record);
        this._parentsByRelationshipField.ToList()
            .ForEach(pair => pair.Key.SetValue(clone, pair.Value[row]));
        this._childrenByRelationshipField.ToList()
            .ForEach(pair => pair.Key.SetValue(clone, ConcreteListOf(pair.Key.PropertyType, pair.Value[row])));
        this.ForcedValuesForRow(row).ToList()
            .ForEach(pair => pair.Key.SetValue(clone, pair.Value));
        return clone;
    }

    private static IList ConcreteListOf(Type listType, List<object> items)
    {
        IList concreteList = (IList)Activator.CreateInstance(listType)!;
        items.ToList().ForEach(item => concreteList.Add(item));
        return concreteList;
    }

    private Dictionary<PropertyInfo, object?> ForcedValuesForRow(int row)
    {
        Dictionary<PropertyInfo, object?> here = this._uniformValueByField.ToDictionary(pair => pair.Key, pair => pair.Value);
        this._perRowValuesByField.ToList().ForEach(pair => here[pair.Key] = pair.Value[row]);
        return here;
    }

    private void RejectMisalignedGrafts()
    {
        int rows = this._records.Count;
        this._parentsByRelationshipField.ToList()
            .ForEach(pair => RejectWrongLength(pair.Key.Name, pair.Value.Count, rows));
        this._childrenByRelationshipField.ToList()
            .ForEach(pair => RejectWrongLength(pair.Key.Name, pair.Value.Count, rows));
        this._perRowValuesByField.ToList()
            .ForEach(pair => RejectWrongLength($"ValuePerRow({pair.Key.Name})", pair.Value.Count, rows));
    }

    private static void RejectWrongLength(string label, int actual, int expected)
    {
        if (actual != expected)
        {
            throw new XftyConfigurationException(
                $"RecordInjector: {label} has {actual} entries but there are {expected} records - grafts must align 1:1 with the records.");
        }
    }
}