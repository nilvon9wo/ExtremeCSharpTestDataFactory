using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core.Core;

/// <summary>
/// Reads one field several relationship hops up a generated ancestor graph,
/// without the caller holding on to every intermediate bundle and list.
///
/// path is one or more relationship fields then the field to read. Returns
/// null when a hop was not generated or rowIndex is out of range. Only walks
/// generated ancestors - child relationships are not followed. Throws only
/// when the path itself is malformed.
/// </summary>
public static class AncestorPathWalker
{
    public static object? Read(Bundle source, List<PropertyInfo> path, int rowIndex)
    {
        RejectMalformed(path);
        Bundle? owningBundle = Descend(source, path);
        bool cannotRead = owningBundle is null || rowIndex < 0;
        return cannotRead
            ? null
            : ValueAt(owningBundle!, path, rowIndex);
    }

    private static Bundle? Descend(Bundle source, List<PropertyInfo> path) =>
        path.Take(path.Count - 2).Aggregate<PropertyInfo, Bundle?>(source, (bundle, hop) => bundle?.GetBundle(hop));

    private static object? ValueAt(Bundle owningBundle, List<PropertyInfo> path, int rowIndex)
    {
        PropertyInfo lastRelationshipField = path[^2];
        PropertyInfo fieldToRead = path[^1];
        List<object>? parents = owningBundle.GetList(lastRelationshipField);
        bool noParentAtRow = parents is null || rowIndex >= parents.Count || parents[rowIndex] is null;
        return noParentAtRow
            ? null
            : fieldToRead.GetValue(parents![rowIndex]);
    }

    private static void RejectMalformed(List<PropertyInfo>? path)
    {
        if (path is null || path.Count < 2)
        {
            throw new XftyConfigurationException("GetValue needs a path of at least one relationship field then the field to read.");
        }

        if (path.Any(step => step is null))
        {
            throw new XftyConfigurationException("GetValue path steps cannot be null.");
        }
    }
}
