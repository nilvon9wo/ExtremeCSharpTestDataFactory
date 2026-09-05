using System.Reflection;
using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>
/// One frame of BundleEnricher's recursive walk. Mutable, but populated only
/// by BundleEnricher's three *Position methods; every other method reads. A
/// top-level class rather than a nested one - this library keeps no nested
/// types.
/// </summary>
internal sealed class EnrichmentPosition
{
    public Bundle? SubBundle { get; }

    public List<object>? Records { get; }

    /// <summary>Null once the walk turns downward.</summary>
    public List<PropertyInfo>? PathFromEntry { get; set; }

    /// <summary>The child hops walked so far; null on an ancestor.</summary>
    public List<PropertyInfo>? ChildPathFromEntry { get; set; }

    public int ParentDepthLeft { get; set; }

    public int ChildDepthLeft { get; set; }

    public bool IsRoot { get; set; }

    public PropertyInfo? InverseChildField { get; private set; }

    public List<List<object>>? InverseChildrenPerRow { get; private set; }

    public EnrichmentPosition(Bundle? subBundle, List<object>? records)
    {
        this.SubBundle = subBundle;
        this.Records = records;
    }

    public void CarryInverse(PropertyInfo childField, List<List<object>> perRow)
    {
        this.InverseChildField = childField;
        this.InverseChildrenPerRow = perRow;
    }

    public Type? PositionType() =>
        this.Records is not { Count: > 0 }
            ? null
            : this.Records[0].GetType();
}
