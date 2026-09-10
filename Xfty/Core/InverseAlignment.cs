using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.Bundles;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>
/// The inverse of the 1:1 parent alignment: for each parent record, the child
/// records whose foreign key points at it. Matched on the parent's primary
/// key when the parents carry a value for it, otherwise position for position
/// (the NEVER / pre-flush case). The parent's key field comes from the
/// Provider (<paramref name="parentPrimaryField"/>); it falls back to a
/// property literally named "Id" only when a caller cannot supply one. Behind
/// <see cref="Bundle.PrimariesResolvingTo"/>.
/// </summary>
public static class InverseAlignment
{
    private const string IdFieldName = "Id";

    public static List<List<object>> ChildrenPerParent(
        List<object> parents,
        List<object> children,
        PropertyInfo relationshipField,
        PropertyInfo? parentPrimaryField = null) =>
        [.. parents.Select((parent, parentRow) =>
            MatchesFor(parent, children, relationshipField, parentRow, parentPrimaryField ?? IdFieldOf(parent)))];

    private static List<object> MatchesFor(
        object parent,
        List<object> children,
        PropertyInfo relationshipField,
        int parentRow,
        PropertyInfo? parentPrimaryField
    ) =>
        parentPrimaryField?.GetValue(parent) is { } parentId
            ? ForeignKeyMatch(children, relationshipField, parentId)
            : PositionMatch(children, parentRow);

    private static PropertyInfo? IdFieldOf(object? record) =>
        record?.GetType().GetProperty(IdFieldName);

    private static List<object> ForeignKeyMatch(
        List<object> children,
        PropertyInfo relationshipField,
        object parentId
    ) =>
        [.. children.Where(child => child is not null && Equals(relationshipField.GetValue(child), parentId))];

    private static List<object> PositionMatch(List<object> children, int parentRow) =>
        parentRow < children.Count
            ? [children[parentRow]]
            : [];
}