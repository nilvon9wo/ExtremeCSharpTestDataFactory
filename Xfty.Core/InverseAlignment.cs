using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The inverse of the 1:1 parent alignment: for each parent record, the child
/// records whose foreign key points at it. Matched on the Id when the parents
/// carry one, otherwise position for position (the NEVER / pre-flush case).
/// Behind <see cref="Bundle.PrimariesResolvingTo"/>.
/// </summary>
public static class InverseAlignment
{
    private const string IdFieldName = "Id";

    public static List<List<object>> ChildrenPerParent(
        List<object> parents,
        List<object> children,
        PropertyInfo relationshipField) =>
        parents
            .Select((parent, parentRow) => MatchesFor(parent, children, relationshipField, parentRow))
            .ToList();

    private static List<object> MatchesFor(object parent, List<object> children, PropertyInfo relationshipField, int parentRow) =>
        IdOf(parent) is { } parentId
            ? ForeignKeyMatch(children, relationshipField, parentId)
            : PositionMatch(children, parentRow);

    private static object? IdOf(object? record) =>
        record?.GetType().GetProperty(IdFieldName)?.GetValue(record);

    private static List<object> ForeignKeyMatch(List<object> children, PropertyInfo relationshipField, object parentId) =>
        children
            .Where(child => child is not null && Equals(relationshipField.GetValue(child), parentId))
            .ToList();

    private static List<object> PositionMatch(List<object> children, int parentRow) =>
        parentRow < children.Count
            ? [children[parentRow]]
            : [];
}
