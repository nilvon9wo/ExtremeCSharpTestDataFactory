using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

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
        return soFar is null
            ? incoming
            : CombinedPrimaries(soFar, incoming);
    }

    private static Bundle CombinedPrimaries(Bundle soFar, Bundle incoming)
    {
        List<object> records = [.. soFar.PrimaryRecords() ?? [], .. incoming.PrimaryRecords() ?? []];
        Bundle rebuilt = new();
        rebuilt.PutPrimaries(incoming.PrimaryTargetField!, records);
        return rebuilt;
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
