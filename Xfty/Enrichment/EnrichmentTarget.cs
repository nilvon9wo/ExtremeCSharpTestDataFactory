using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.Enrichment;

/// <summary>
/// Resolves the field passed to bundle.Inject(field, ...) to the list of
/// records to enrich, the sub-bundle carrying their relationships, and
/// whether those records are a generated ancestor (so their inverse child
/// can be grafted). An unknown field is a loud error naming the fields the
/// bundle actually holds.
/// </summary>
public sealed class EnrichmentTarget
{
    public List<object>? Records { get; }

    public Bundle? SubBundle { get; }

    public bool IsGeneratedAncestor { get; }

    private EnrichmentTarget(List<object>? records, Bundle? subBundle, bool isGeneratedAncestor)
    {
        this.Records = records;
        this.SubBundle = subBundle;
        this.IsGeneratedAncestor = isGeneratedAncestor;
    }

    public static EnrichmentTarget Locate(Bundle bundle, PropertyInfo field) =>
        field == bundle.PrimaryTargetField
            ? new EnrichmentTarget(bundle.PrimaryRecords(), bundle, false)
            : bundle.RelationshipFields().Contains(field)
                ? new EnrichmentTarget(bundle.GetList(field), bundle.GetBundle(field), true)
                : bundle.ChildRelationshipFields().Contains(field)
                    ? new EnrichmentTarget(bundle.GetChildList(field), bundle.GetChildBundle(field), false)
                    : throw new XftyConfigurationException(
                        $"Inject: {field.Name} is not this bundle's primary field, a generated ancestor field "
                        + $"[{string.Join(", ", bundle.RelationshipFields().Select(f => f.Name))}], or a child field "
                        + $"[{string.Join(", ", bundle.ChildRelationshipFields().Select(f => f.Name))}].");

    /// <summary>True when the graph has any generated ancestor or child collection to inject.</summary>
    public bool HasAnythingToInject()
    {
        bool hasParents = this.SubBundle is not null && this.SubBundle.RelationshipFields().Count > 0;
        bool hasChildren = (this.SubBundle is not null && this.SubBundle.ChildRelationshipFields().Count > 0) || this.IsGeneratedAncestor;
        return hasParents || hasChildren;
    }
}
