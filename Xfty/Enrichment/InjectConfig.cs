using System.Reflection;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>
/// What <c>bundle.Inject(field, config)</c> should materialise. Start from a
/// breadth - <see cref="Nothing"/> (name everything you want), <see cref="AllParents"/>,
/// <see cref="AllChildren"/>, <see cref="Everything"/> - then layer refiners on:
///
/// - InjectParent(path) - inject the ancestor at path (relationship hops from
///   the target record) and every hop along the way;
/// - InjectChild(childField) - inject that one child collection (one hop) on the
///   target records;
/// - ExcludeParent(path) / ExcludeChild(childField) - prune from a breadth start;
/// - InjectValue(field, value) - a scalar on the target record itself
///   (formula / roll-up / system / read-only);
/// - InjectValue(path, value) - a scalar on a record several hops up, where
///   path is the relationship hops then the target field;
/// - InjectChildValue(childField, leafField, value) / InjectChildValue(path, value) -
///   a scalar on the records of a child collection (or grandchild), path being
///   the child-lookup hops read downward then the field to set. value is a
///   literal (every child gets it), a List&lt;object&gt; (one per child, in
///   GetChildList order), or an IValueExpression (resolved fresh per child);
/// - ParentDepth(n) - cap the ancestor climb (default 5, the SOQL limit);
/// - ChildDepth(n) - how many levels of nested child collections (default 1;
///   n &gt; 1 needs BreakSoqlLimits());
/// - BreakSoqlLimits() - lift the ceiling on ParentDepth, ChildDepth, and the
///   InjectParent path length.
///
/// A plain state carrier - the enricher reads the fields, nothing here acts.
/// <c>bundle.InjectAll(field)</c> is <c>Inject(field, Everything())</c>.
/// </summary>
public sealed class InjectConfig
{
    public const int SoqlParentHops = 5;
    public const int SoqlChildDepth = 1;

    public bool FromAllParents { get; }

    public bool FromAllChildren { get; }

    public List<List<PropertyInfo>> IncludedParentPaths { get; } = [];

    public List<List<PropertyInfo>> ExcludedParentPaths { get; } = [];

    public HashSet<PropertyInfo> IncludedChildFields { get; } = [];

    public HashSet<PropertyInfo> ExcludedChildFields { get; } = [];

    public Dictionary<PropertyInfo, object?> OnRecordValues { get; } = [];

    public List<AncestorValue> AncestorValues { get; } = [];

    public List<ChildValue> ChildValues { get; } = [];

    public int ParentDepthLimit { get; private set; } = SoqlParentHops;

    public int ChildDepthLimit { get; private set; } = SoqlChildDepth;

    public bool SoqlLimitsLifted { get; private set; }

    private InjectConfig(bool fromAllParents, bool fromAllChildren)
    {
        this.FromAllParents = fromAllParents;
        this.FromAllChildren = fromAllChildren;
    }

    /// <summary>Name every parent and child to inject; nothing is materialised by default.</summary>
    public static InjectConfig Nothing() => new(false, false);

    /// <summary>Every generated ancestor, to ParentDepth; children only if named.</summary>
    public static InjectConfig AllParents() => new(true, false);

    /// <summary>Every generated child collection, to ChildDepth; parents only if named.</summary>
    public static InjectConfig AllChildren() => new(false, true);

    /// <summary>Every generated ancestor and child collection. bundle.InjectAll(field) uses this.</summary>
    public static InjectConfig Everything() => new(true, true);

    public InjectConfig InjectParent(List<PropertyInfo> path)
    {
        this.IncludedParentPaths.Add(path);
        return this;
    }

    public InjectConfig ExcludeParent(List<PropertyInfo> path)
    {
        this.ExcludedParentPaths.Add(path);
        return this;
    }

    /// <summary>Inject the child collection this lookup field defines (e.g. Contact.AccountId -&gt; the Account's Contacts).</summary>
    public InjectConfig InjectChild(PropertyInfo childLookupField)
    {
        _ = this.IncludedChildFields.Add(childLookupField);
        return this;
    }

    public InjectConfig ExcludeChild(PropertyInfo childLookupField)
    {
        _ = this.ExcludedChildFields.Add(childLookupField);
        return this;
    }

    /// <summary>A scalar on the target record - the formula / roll-up / system / read-only case.</summary>
    public InjectConfig InjectValue(PropertyInfo field, object? value)
    {
        this.OnRecordValues[field] = value;
        return this;
    }

    /// <summary>A scalar on a record several relationship hops up - path is the hops then the target field.</summary>
    public InjectConfig InjectValue(List<PropertyInfo> pathToField, object? value)
    {
        this.AncestorValues.Add(new AncestorValue(pathToField, value));
        return this;
    }

    /// <summary>
    /// A scalar on every record of the child collection childField defines
    /// (Contact.AccountId -&gt; the target's Contacts). value is a literal, a
    /// List&lt;object&gt; (one per child), or an IValueExpression.
    /// </summary>
    public InjectConfig InjectChildValue(PropertyInfo childField, PropertyInfo leafField, object? value) =>
        this.InjectChildValue([childField, leafField], value);

    /// <summary>
    /// A scalar on a child (or grandchild) record - path is the child-lookup
    /// hops read downward, then the field to set. Same value forms as the
    /// three-argument overload.
    /// </summary>
    public InjectConfig InjectChildValue(List<PropertyInfo> pathToLeaf, object? value)
    {
        this.ChildValues.Add(new ChildValue(pathToLeaf, value));
        return this;
    }

    public InjectConfig ParentDepth(int hops)
    {
        this.ParentDepthLimit = hops;
        return this;
    }

    /// <summary>How many levels of nested child collections. Default 1; hops &gt; 1 needs BreakSoqlLimits().</summary>
    public InjectConfig ChildDepth(int hops)
    {
        this.ChildDepthLimit = hops;
        return this;
    }

    /// <summary>Allow ParentDepth, ChildDepth and the InjectParent path length to exceed what one SOQL could return.</summary>
    public InjectConfig BreakSoqlLimits()
    {
        this.SoqlLimitsLifted = true;
        return this;
    }
}
