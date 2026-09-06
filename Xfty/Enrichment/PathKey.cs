using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Enrichment;

/// <summary>
/// A stable, comparable key for a relationship path - the declaring type and
/// name of each field, joined by "&gt;", "" for an empty or null path. Used to
/// match and de-duplicate paths without a PropertyInfo-keyed collection.
/// </summary>
public static class PathKey
{
    public static string Of(List<PropertyInfo>? path) =>
        path is null
            ? string.Empty
            : string.Concat(path.Select(step => $"{step.DeclaringType?.FullName}.{step.Name}>"));
}
