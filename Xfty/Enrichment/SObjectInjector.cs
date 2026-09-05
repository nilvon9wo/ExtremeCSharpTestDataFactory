using System.Collections;
using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>
/// Writes onto record instances what an <c>init</c>-only property rejects
/// after construction - a populated parent relationship, a child collection,
/// a forced scalar - via reflection, one clone per row.
///
/// Apex's original round-tripped the whole list through JSON.serialize /
/// JSON.deserialize, because SObject.put(...) rejects relationship and
/// read-only fields outright; that mechanism (and XFTY_BlobCarrier, which
/// exists only to shepherd a Blob through that JSON round-trip) has no
/// reason to exist here - reflection sets any property directly, so a Blob-
/// shaped field needs no special-casing. Per-record: SetValue bypasses
/// init-only the same way IdMocker and RecordCloneFactory already rely on.
///
/// Standalone: no bundle, no generation. Collect the grafts fluently, then
/// Result() clones every row once and applies them all. Inputs are
/// untouched; the returned list is new instances.
///
/// <code>
/// List&lt;object&gt; withAccount = SObjectInjector.Inject(contacts)
///     .Relationship(Field.Of&lt;Contact&gt;(nameof(Contact.Account)), accountsAligned1to1)
///     .Value(Field.Of&lt;Contact&gt;(nameof(Contact.Id)), someId)
///     .Result();
/// </code>
/// </summary>
public sealed class SObjectInjector
{
    private readonly List<object> records;
    private readonly Dictionary<PropertyInfo, List<object>> parentsByRelationshipField = [];
    private readonly Dictionary<PropertyInfo, List<List<object>>> childrenByRelationshipField = [];
    private readonly Dictionary<PropertyInfo, object?> uniformValueByField = [];
    private readonly Dictionary<PropertyInfo, List<object?>> perRowValuesByField = [];

    private SObjectInjector(List<object> records) =>
        this.records = records ?? throw new XftyConfigurationException("SObjectInjector needs a records list, not null.");

    public static SObjectInjector Inject(List<object> records) => new(records);

    /// <summary>Graft parents[row] onto records[row] under relationshipField (e.g. Contact.Account).</summary>
    public SObjectInjector Relationship(PropertyInfo relationshipField, List<object> parents)
    {
        this.parentsByRelationshipField[relationshipField] = parents;
        return this;
    }

    /// <summary>Graft childrenPerRow[row] onto records[row] as relationshipField's collection (e.g. Account.Contacts).</summary>
    public SObjectInjector ChildRelationship(PropertyInfo relationshipField, List<List<object>> childrenPerRow)
    {
        this.childrenByRelationshipField[relationshipField] = childrenPerRow;
        return this;
    }

    /// <summary>Set field to the same value on every row.</summary>
    public SObjectInjector Value(PropertyInfo field, object? valueForEveryRow)
    {
        this.uniformValueByField[field] = valueForEveryRow;
        return this;
    }

    /// <summary>Set field to values[row] on each row.</summary>
    public SObjectInjector ValuePerRow(PropertyInfo field, List<object?> values)
    {
        this.perRowValuesByField[field] = values;
        return this;
    }

    public List<object> Result()
    {
        if (this.records.Count == 0)
        {
            return [];
        }

        this.RejectMisalignedGrafts();
        return this.records.Select(this.GraftedRow).ToList();
    }

    private object GraftedRow(object record, int row)
    {
        object clone = RecordCloneFactory.DeepClone(record);
        this.parentsByRelationshipField.ToList()
            .ForEach(pair => pair.Key.SetValue(clone, pair.Value[row]));
        this.childrenByRelationshipField.ToList()
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
        Dictionary<PropertyInfo, object?> here = this.uniformValueByField.ToDictionary(pair => pair.Key, pair => pair.Value);
        this.perRowValuesByField.ToList().ForEach(pair => here[pair.Key] = pair.Value[row]);
        return here;
    }

    private void RejectMisalignedGrafts()
    {
        int rows = this.records.Count;
        this.parentsByRelationshipField.ToList()
            .ForEach(pair => RejectWrongLength(pair.Key.Name, pair.Value.Count, rows));
        this.childrenByRelationshipField.ToList()
            .ForEach(pair => RejectWrongLength(pair.Key.Name, pair.Value.Count, rows));
        this.perRowValuesByField.ToList()
            .ForEach(pair => RejectWrongLength($"ValuePerRow({pair.Key.Name})", pair.Value.Count, rows));
    }

    private static void RejectWrongLength(string label, int actual, int expected)
    {
        if (actual != expected)
        {
            throw new XftyConfigurationException(
                $"SObjectInjector: {label} has {actual} entries but there are {expected} records - grafts must align 1:1 with the records.");
        }
    }
}
