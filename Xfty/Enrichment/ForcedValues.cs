using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Enrichment;

/// <summary>
/// Applies the forced scalar values an InjectConfig carries - InjectValue(field, v)
/// on the target record, InjectValue(path, v) on a record several hops up,
/// InjectChildValue(path, v) on the records of a child collection - onto the
/// SObjectInjector for whichever position BundleEnricher is enriching. No
/// recursion; just value placement.
///
/// Each value may be a literal (every record at the position gets it), a
/// List&lt;object&gt; (one per record, in position order), or an IValueExpression
/// (resolved fresh per record - so an incrementing expression gives each child
/// a distinct value).
///
/// AssertEveryPathWasReached is the safety net: a path that never matched a
/// visited position - a typo, an ancestor that was not generated, a child
/// collection the walk never descended into - is a loud error, not a silent no-op.
/// </summary>
public sealed class ForcedValues
{
    private readonly InjectConfig config;
    private readonly HashSet<int> reachedAncestorValues = [];
    private readonly HashSet<int> reachedChildValues = [];

    public ForcedValues(InjectConfig config) => this.config = config;

    /// <summary>The InjectValue(field, v) scalars - the target record itself.</summary>
    public void ApplyRecordValues(SObjectInjector injector, int rowCount) =>
        PlaceAll(injector, this.config.OnRecordValues, rowCount);

    /// <summary>The InjectValue(path, v) scalars whose relationship prefix is this ancestor position.</summary>
    public void ApplyAncestorValues(SObjectInjector injector, List<PropertyInfo> pathFromEntry, int rowCount)
    {
        string hereKey = PathKey.Of(pathFromEntry);
        Dictionary<PropertyInfo, object?> matched = [];
        this.config.AncestorValues
            .Select((ancestorValue, index) => (ancestorValue, index))
            .Where(pair => PathKey.Of(pair.ancestorValue.RelationshipPrefix()) == hereKey)
            .ToList()
            .ForEach(pair =>
            {
                matched[pair.ancestorValue.TargetField()] = pair.ancestorValue.Value;
                _ = this.reachedAncestorValues.Add(pair.index);
            });
        PlaceAll(injector, matched, rowCount);
    }

    /// <summary>The InjectChildValue(path, v) scalars whose relationship prefix is this child position.</summary>
    public void ApplyChildValues(SObjectInjector injector, List<PropertyInfo> childPathFromEntry, int rowCount)
    {
        string hereKey = PathKey.Of(childPathFromEntry);
        Dictionary<PropertyInfo, object?> matched = [];
        this.config.ChildValues
            .Select((childValue, index) => (childValue, index))
            .Where(pair => PathKey.Of(pair.childValue.RelationshipPrefix()) == hereKey)
            .ToList()
            .ForEach(pair =>
            {
                matched[pair.childValue.TargetField()] = pair.childValue.Value;
                _ = this.reachedChildValues.Add(pair.index);
            });
        PlaceAll(injector, matched, rowCount);
    }

    /// <summary>Throw if any InjectValue(path) / InjectChildValue never matched a visited position.</summary>
    public void AssertEveryPathWasReached()
    {
        List<string> unreached =
        [
            .. this.config.AncestorValues
                .Select((value, index) => (value, index))
                .Where(pair => !this.reachedAncestorValues.Contains(pair.index))
                .Select(pair => $"InjectValue {PathKey.Of(pair.value.Path)}"),
            .. this.config.ChildValues
                .Select((value, index) => (value, index))
                .Where(pair => !this.reachedChildValues.Contains(pair.index))
                .Select(pair => $"InjectChildValue {PathKey.Of(pair.value.Path)}"),
        ];
        if (unreached.Count > 0)
        {
            throw new XftyConfigurationException(
                $"Inject: [{string.Join(", ", unreached)}] named a record the graph never produced or the walk "
                + "never reached (check the path, that the ancestor / child was generated, and ParentDepth / ChildDepth).");
        }
    }

    private static void PlaceAll(SObjectInjector injector, Dictionary<PropertyInfo, object?> valueByField, int rowCount) =>
        valueByField.ToList().ForEach(pair => Place(injector, pair.Key, pair.Value, rowCount));

    private static void Place(SObjectInjector injector, PropertyInfo field, object? value, int rowCount) =>
        _ = value switch
        {
            List<object?> perRow => injector.ValuePerRow(field, perRow),
            IValueExpression expression => injector.ValuePerRow(field, ResolvedPerRow(expression, rowCount)),
            _ => injector.Value(field, value),
        };

    private static List<object?> ResolvedPerRow(IValueExpression expression, int rowCount) =>
        Enumerable.Range(0, rowCount).Select(_ => expression.Get()).ToList();
}
