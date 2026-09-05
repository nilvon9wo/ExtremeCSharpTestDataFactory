using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The record(s) one <c>CreateBundle</c> call has produced: primary records
/// plus their generated relationships (parents) and children.
///
/// Enrichment (<c>Inject</c>/<c>InjectAll</c>/etc. in the Apex original) is
/// not ported here yet - it's a reflection-based rebuild of its own, tracked
/// separately (see csharp-port-idea.md).
/// </summary>
public sealed class Bundle
{
    private readonly Dictionary<PropertyInfo, Bundle> sObjectBundleByField = new();
    private readonly Dictionary<PropertyInfo, List<object>> sObjectListByField = new();
    private readonly Dictionary<PropertyInfo, List<BundleChildEntry>> childEntriesByRelationshipField = new();
    private readonly DeferredValueQueue deferredValueQueue = new();

    /// <summary>The field this bundle's primary records are keyed under. Null on a bundle built by hand.</summary>
    public PropertyInfo? PrimaryTargetField { get; private set; }

    public Bundle Put(PropertyInfo field, List<object> records)
    {
        this.sObjectListByField[field] = records;
        return this;
    }

    public Bundle Put(PropertyInfo field, Bundle bundle)
    {
        this.sObjectBundleByField[field] = bundle;
        return this;
    }

    public Bundle? GetBundle(PropertyInfo field) =>
        this.sObjectBundleByField.GetValueOrDefault(field);

    public List<object>? GetList(PropertyInfo field) =>
        this.sObjectListByField.GetValueOrDefault(field);

    /// <summary>Read one field several relationship hops up the generated ancestor graph. See <see cref="AncestorPathWalker"/>.</summary>
    public object? GetValue(List<PropertyInfo> path, int rowIndex) =>
        AncestorPathWalker.Read(this, path, rowIndex);

    /// <summary>GetValue(path, 0) - for a single-primary bundle.</summary>
    public object? GetValue(List<PropertyInfo> path) =>
        this.GetValue(path, 0);

    /// <summary>Record the primary records and the field they belong to.</summary>
    public void PutPrimaries(PropertyInfo primaryTargetField, List<object> records)
    {
        this.PrimaryTargetField = primaryTargetField;
        _ = this.Put(primaryTargetField, records);
    }

    /// <summary>The records this bundle is about; null when PrimaryTargetField is not set.</summary>
    public List<object>? PrimaryRecords() =>
        this.PrimaryTargetField is null
            ? null
            : this.GetList(this.PrimaryTargetField);

    /// <summary>The relationship fields that carry a generated sub-bundle (the parents).</summary>
    public ISet<PropertyInfo> RelationshipFields() =>
        this.sObjectBundleByField.Keys.ToHashSet();

    /// <summary>
    /// The primary records generated pointing at getList(relationshipField) row
    /// ancestorRowIndex - the inverse of the 1:1 parent alignment, so a shared
    /// ancestor returns the several primaries that resolved to it.
    /// </summary>
    public List<object> PrimariesResolvingTo(PropertyInfo relationshipField, int ancestorRowIndex)
    {
        List<object>? ancestors = this.GetList(relationshipField);
        bool cannotResolve = this.PrimaryRecords() is null
            || ancestors is null
            || ancestorRowIndex < 0
            || ancestorRowIndex >= ancestors.Count;
        return cannotResolve
            ? []
            : InverseAlignment.ChildrenPerParent(ancestors!, this.PrimaryRecords()!, relationshipField)[ancestorRowIndex];
    }

    /// <summary>Record that each primary row's byField entries are still to be resolved up from descendants.</summary>
    public void DeferValues(Dictionary<PropertyInfo, IDeferredExpression> byField) =>
        this.deferredValueQueue.AddForEachRow(this.PrimaryRecords()!.Count, byField);

    public List<BundleDeferredEntry> DeferredValues() =>
        this.deferredValueQueue.Entries();

    public Bundle PutChild(PropertyInfo childRelationshipField, Bundle childBundle, List<int> parentRowByChildRow)
    {
        if (!this.childEntriesByRelationshipField.TryGetValue(childRelationshipField, out List<BundleChildEntry>? entries))
        {
            entries = [];
            this.childEntriesByRelationshipField[childRelationshipField] = entries;
        }

        entries.Add(new BundleChildEntry(childBundle, parentRowByChildRow));
        return this;
    }

    /// <summary>Every child relationship field this bundle carries children for.</summary>
    public ISet<PropertyInfo> ChildRelationshipFields() =>
        this.childEntriesByRelationshipField.Keys.ToHashSet();

    /// <summary>The configured child collections for a relationship field, in declaration order (empty if none).</summary>
    public List<BundleChildEntry> ChildEntries(PropertyInfo childRelationshipField) =>
        this.childEntriesByRelationshipField.GetValueOrDefault(childRelationshipField) ?? [];

    /// <summary>The sub-bundles for a child relationship field, in config declaration order (empty list if none).</summary>
    public List<Bundle> ChildBundles(PropertyInfo childRelationshipField) =>
        this.ChildEntries(childRelationshipField).Select(entry => entry.Bundle).ToList();

    /// <summary>The first child generated for childRelationshipField; null if none.</summary>
    public object? GetChild(PropertyInfo childRelationshipField)
    {
        List<object> all = this.GetChildList(childRelationshipField);
        return all.Count == 0
            ? null
            : all[0];
    }

    /// <summary>Every child generated for childRelationshipField, merged across configs, in the documented order.</summary>
    public List<object> GetChildList(PropertyInfo childRelationshipField) =>
        this.ChildBundles(childRelationshipField)
            .SelectMany(childBundle => childBundle.PrimaryRecords() ?? [])
            .ToList();

    /// <summary>Just the children of one primary row - the slice of GetChildList that belongs to that row.</summary>
    public List<object> ChildRecordsOf(int parentRowIndex, PropertyInfo childRelationshipField) =>
        this.ChildEntries(childRelationshipField)
            .SelectMany(entry => (entry.Bundle.PrimaryRecords() ?? [])
                .Where((_, childRow) => entry.ParentRowByChildRow[childRow] == parentRowIndex))
            .ToList();

    /// <summary>A single bundle of every child for childRelationshipField - merged primaries plus each child's own generated parents. Null if none.</summary>
    public Bundle? GetChildBundle(PropertyInfo childRelationshipField)
    {
        List<Bundle> bundles = this.ChildBundles(childRelationshipField);
        return bundles.Count switch
        {
            0 => null,
            1 => bundles[0],
            _ => BundleMerger.Combine(bundles),
        };
    }
}
