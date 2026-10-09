using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Core.Bundles;

/// <summary>
/// Combines the sibling child bundles one relationship field can carry - one
/// per configured child provider - into a single bundle: every child across
/// the configs as primaries, and each generated parent merged so the whole
/// collection navigates as one unit.
/// </summary>
public static class BundleMerger
{
    public static Bundle Combine(List<Bundle> bundles)
    {
        Bundle merged = new();
        bundles.ForEach(each => CombineParentsInto(merged, each));
        PutMergedPrimaries(merged, bundles);
        return merged;
    }

    private static void CombineParentsInto(Bundle merged, Bundle source) =>
        source.RelationshipFields()
            .ToList()
            .ForEach(parentField => merged.Put(parentField, ResolveCombined(merged, source, parentField)));

    private static Bundle ResolveCombined(Bundle merged, Bundle source, PropertyInfo parentField)
    {
        Bundle? soFar = merged.GetBundle(parentField);
        Bundle incoming = source.GetBundle(parentField)!;
        // Two configs' generated parents for the same field merge exactly like the children did - primaries
        // concatenated, and the parents' own generated parents carried along rather than dropped.
        return soFar is null
            ? incoming
            : Combine([soFar, incoming]);
    }

    private static void PutMergedPrimaries(Bundle merged, List<Bundle> bundles)
    {
        PropertyInfo? primaryField = bundles[0].PrimaryTargetField;
        if (primaryField is null)
        {
            return;
        }

        List<object> allPrimaries = [.. bundles.SelectMany(each => each.PrimaryRecords() ?? [])];
        merged.PutPrimaries(primaryField, allPrimaries);
    }
}