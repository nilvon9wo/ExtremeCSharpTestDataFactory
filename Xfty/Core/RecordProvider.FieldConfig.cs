using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>RecordProvider - field/relationship configuration, delegated to <see cref="RecordProviderTemplateConfig"/>.</summary>
public sealed partial class RecordProvider
{
    public RecordProvider Put(PropertyInfo field, IValueExpression valueTemplate) => this.PutValue(field, valueTemplate);

    public RecordProvider Put(PropertyInfo field, IContextAwareExpression contextAwareExpression) => this.PutValue(field, contextAwareExpression);

    /// <summary>An up-flowing value; needs the DEFERRED insert mode.</summary>
    public RecordProvider Put(PropertyInfo field, IDeferredExpression deferredValue) => this.PutValue(field, deferredValue);

    /// <summary>Convenience overload mirroring MasterTemplate.Put(field, object): routed by runtime type.</summary>
    public RecordProvider Put(PropertyInfo field, object? value) => this.PutValue(field, value);

    private RecordProvider PutValue(PropertyInfo field, object? value)
    {
        this.templateConfig.Put(field, value);
        return this;
    }

    public RecordProvider PutRequired(PropertyInfo field, IDefaultRelationship relationshipTemplate)
    {
        this.templateConfig.PutRequired(field, relationshipTemplate);
        return this;
    }

    public RecordProvider PutOptional(PropertyInfo field, IDefaultRelationship relationshipTemplate)
    {
        this.templateConfig.PutOptional(field, relationshipTemplate);
        return this;
    }

    public RecordProvider RemoveFromMasterTemplate(PropertyInfo field)
    {
        this.templateConfig.RemoveFromMasterTemplate(field);
        return this;
    }

    // Per-call relationship control ---------------------------------

    /// <summary>Generate one specific relationship on this call, on top of whatever SetInclusivity(...) covers.</summary>
    public RecordProvider IncludeOptional(PropertyInfo field) => this.IncludeOptional([field]);

    /// <summary>Reach down the graph: force every relationship along the path for this call.</summary>
    public RecordProvider IncludeOptional(List<PropertyInfo> relationshipPath)
    {
        this.templateConfig.IncludeOptional(relationshipPath);
        return this;
    }

    /// <summary>Do not generate one specific relationship on this call - required or optional.</summary>
    public RecordProvider ExcludeRelationship(PropertyInfo field)
    {
        this.templateConfig.ExcludeRelationship(field, this.recordType);
        return this;
    }

    /// <summary>Like ExcludeRelationship, but a no-op when the field is not a relationship on this Provider.</summary>
    public RecordProvider ExcludeRelationshipIfPresent(PropertyInfo field)
    {
        this.templateConfig.ExcludeRelationshipIfPresent(field);
        return this;
    }

    // Path-scoped value overrides -------------------------------------

    public RecordProvider Put(List<PropertyInfo> path, IValueExpression valueExpression) =>
        this.PutPathValue(PathValue.OfExpression(path, valueExpression));

    public RecordProvider Put(List<PropertyInfo> path, IContextAwareExpression contextAwareExpression) =>
        this.PutPathValue(PathValue.OfContextAware(path, contextAwareExpression));

    public RecordProvider Put(List<PropertyInfo> path, object? literal) =>
        this.PutPathValue(PathValue.OfLiteral(path, literal));

    public RecordProvider PutRequired(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.PutPathValue(PathValue.OfRequiredRelationship(path, relationship));

    public RecordProvider PutOptional(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.PutPathValue(PathValue.OfOptionalRelationship(path, relationship));

    private RecordProvider PutPathValue(PathValue pathValue)
    {
        this.templateConfig.AddPathValue(pathValue);
        return this;
    }
}
