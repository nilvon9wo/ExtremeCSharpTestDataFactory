using System.Reflection;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>
/// Collects the records of one or more generated-but-unsaved bundles into
/// the flat list + parent links <see cref="DepthBatchedInserter"/> needs, and
/// saves them.
///
/// Each bundle is a tree - the engine clones every template and generates a
/// distinct parent per child row - so a record never appears twice and the
/// walk needs no identity tracking. Just before the insert, the up-flow
/// value pass (<see cref="DescendantValuePass"/>) fills any
/// CopyFromDescendantExpression field from its now-generated children.
/// </summary>
public sealed class DeferredInsertBuffer
{
    private readonly List<DepthBatchedInserterParentLink> pendingLinks = [];
    private readonly List<object> pendingRecords = [];
    private readonly List<PendingDeferredValue> pendingDeferredValues = [];

    public static void InsertGraph(Bundle? bundle)
    {
        DeferredInsertBuffer buffer = new();
        buffer.Add(bundle);
        buffer.InsertAll();
    }

    /// <summary>The whole graph flattened to its records and parent links, with the up-flow value pass already run.</summary>
    public static DeferredInsertBuffer Flatten(Bundle? bundle)
    {
        DeferredInsertBuffer buffer = new();
        buffer.Add(bundle);
        buffer.ResolveUpFlowValues();
        return buffer;
    }

    public void Add(Bundle? bundle) => this.Collect(bundle);

    public int PendingCount() => this.pendingRecords.Count;

    /// <summary>Every record the graph holds, in collection order (not insert order).</summary>
    public List<object> Records() => this.pendingRecords;

    /// <summary>Each record's lookup to another record in Records(), by index.</summary>
    public List<DepthBatchedInserterParentLink> ParentLinks() => this.pendingLinks;

    public void InsertAll()
    {
        this.ResolveUpFlowValues();
        DepthBatchedInserter.InsertAll(this.pendingRecords, this.pendingLinks);
    }

    /// <summary>Depth-batched resolution of every buffered bundle honouring mode (Now/Mock/Never).</summary>
    public void ResolveAll(InsertMode mode)
    {
        this.ResolveUpFlowValues();
        DepthBatchedInserter.ResolveAll(this.pendingRecords, this.pendingLinks, mode);
    }

    private void ResolveUpFlowValues() =>
        new DescendantValuePass(this.pendingRecords, this.pendingLinks, this.pendingDeferredValues).Complete();

    private List<IndexedRecord> Collect(Bundle? bundle)
    {
        List<object>? primaries = PrimaryRecordsOf(bundle);
        if (primaries is null)
        {
            return [];
        }

        List<IndexedRecord> theseRecords = this.Append(primaries);
        this.CaptureDeferredValues(bundle!, theseRecords);
        this.LinkToParents(bundle!, theseRecords);
        this.LinkToChildCollections(bundle!, theseRecords);
        return theseRecords;
    }

    /// <summary>An up-flow strategy on this bundle becomes a pending value keyed by the record's flat index.</summary>
    private void CaptureDeferredValues(Bundle bundle, List<IndexedRecord> primaries) =>
        bundle.DeferredValues().ForEach(deferred =>
            this.pendingDeferredValues.Add(
                new PendingDeferredValue(primaries[deferred.PrimaryRow].Index, deferred.Field, deferred.Strategy)));

    /// <summary>Downward children (With(...)/WithChildren(...)): each child row points at its primary row.</summary>
    private void LinkToChildCollections(Bundle bundle, List<IndexedRecord> primaries) =>
        bundle.ChildRelationshipFields().ToList().ForEach(childField => this.LinkChildField(bundle, primaries, childField));

    private void LinkChildField(Bundle bundle, List<IndexedRecord> primaries, PropertyInfo childField) =>
        bundle.ChildEntries(childField).ForEach(entry => this.LinkChildEntry(entry, primaries, childField));

    private void LinkChildEntry(BundleChildEntry entry, List<IndexedRecord> primaries, PropertyInfo childField)
    {
        List<IndexedRecord> childRecords = this.Collect(entry.Bundle);
        childRecords
            .Select((childRecord, childRow) => (childRecord, childRow))
            .ToList()
            .ForEach(each => this.LinkChild(each.childRecord, primaries[entry.ParentRowByChildRow[each.childRow]], childField));
    }

    private void LinkToParents(Bundle bundle, List<IndexedRecord> children) =>
        bundle.RelationshipFields().ToList().ForEach(parentField => this.LinkToParentsOn(bundle, children, parentField));

    private void LinkToParentsOn(Bundle bundle, List<IndexedRecord> children, PropertyInfo parentField)
    {
        Bundle? parentBundle = bundle.GetBundle(parentField);
        if (parentBundle is null)
        {
            return;
        }

        List<IndexedRecord> parents = this.Collect(parentBundle);
        this.LinkRows(children, parents, parentField);
    }

    private void LinkRows(List<IndexedRecord> children, List<IndexedRecord> parents, PropertyInfo field)
    {
        if (parents.Count == 0)
        {
            return;
        }

        // One parent for many children is a shared ancestor collapsed to a single row - every child points at it.
        if (parents.Count == 1 && children.Count > 1)
        {
            children.ForEach(child => this.LinkChild(child, parents[0], field));
            return;
        }

        int rows = Math.Min(children.Count, parents.Count);
        Enumerable.Range(0, rows).ToList().ForEach(row => this.LinkChild(children[row], parents[row], field));
    }

    private void LinkChild(IndexedRecord child, IndexedRecord parent, PropertyInfo field)
    {
        if (field.GetValue(child.Record) is not null)
        {
            return;
        }

        this.pendingLinks.Add(new DepthBatchedInserterParentLink(child.Index, parent.Index, field));
    }

    private List<IndexedRecord> Append(List<object> records) => records.Select(this.AppendOne).ToList();

    private IndexedRecord AppendOne(object record)
    {
        int index = this.pendingRecords.Count;
        this.pendingRecords.Add(record);
        return new IndexedRecord(index, record);
    }

    private static List<object>? PrimaryRecordsOf(Bundle? bundle) => bundle?.PrimaryRecords();
}
