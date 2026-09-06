using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>
/// The field/relationship configuration one <see cref="RecordProvider"/> call
/// accumulates: direct Put(...)/PutRequired(...)/PutOptional(...) on its own
/// Master Template, per-call relationship overrides (IncludeOptional,
/// ExcludeRelationship), and path-scoped value overrides on a generated
/// ancestor. Extracted so RecordProvider itself stays a thin fluent facade.
/// </summary>
internal sealed class RecordProviderTemplateConfig(Func<MasterTemplate> resolveBaseTemplate)
{
    private MasterTemplate? ownTemplate;

    public bool HasCustomTemplate { get; private set; }

    public List<List<PropertyInfo>> ForcedRelationshipPaths { get; } = [];

    public List<PathValue> PathValues { get; } = [];

    public MasterTemplate ResolveTemplate() =>
        this.ownTemplate ??= resolveBaseTemplate();

    public void Put(PropertyInfo field, object? value) =>
        this.Mutate(() => this.ResolveTemplate().Put(field, value));

    public void PutRequired(PropertyInfo field, IDefaultRelationship relationship) =>
        this.Mutate(() => this.ResolveTemplate().Remove(field).PutRequired(field, relationship));

    public void PutOptional(PropertyInfo field, IDefaultRelationship relationship) =>
        this.Mutate(() => this.ResolveTemplate().Remove(field).PutOptional(field, relationship));

    public void RemoveFromMasterTemplate(PropertyInfo field) =>
        this.Mutate(() => this.ResolveTemplate().Remove(field));

    public void IncludeOptional(List<PropertyInfo> relationshipPath)
    {
        this.AssertValidPath(relationshipPath);
        this.ForcedRelationshipPaths.Add(relationshipPath);
    }

    private void AssertValidPath(List<PropertyInfo> relationshipPath)
    {
        bool isValid = relationshipPath is { Count: > 0 } && relationshipPath.TrueForAll(step => step is not null);
        if (!isValid)
        {
            throw new XftyConfigurationException("IncludeOptional(...) needs at least one non-null relationship field.");
        }
    }

    public void ExcludeRelationship(PropertyInfo field, Type recordType)
    {
        this.AssertIsRelationship(field, recordType);
        this.Mutate(() => this.ResolveTemplate().Remove(field));
    }

    public void ExcludeRelationshipIfPresent(PropertyInfo field)
    {
        if (this.IsRelationshipOnTemplate(field))
        {
            this.Mutate(() => this.ResolveTemplate().Remove(field));
        }
    }

    private void AssertIsRelationship(PropertyInfo field, Type recordType)
    {
        if (!this.IsRelationshipOnTemplate(field))
        {
            throw new XftyConfigurationException($"ExcludeRelationship({field.Name}): {recordType} has no relationship on that field.");
        }
    }

    private bool IsRelationshipOnTemplate(PropertyInfo field)
    {
        MasterTemplate current = this.ResolveTemplate();
        return current.RequiredRelationshipByField.ContainsKey(field) || current.OptionalRelationshipByField.ContainsKey(field);
    }

    public void AddPathValue(PathValue pathValue) => this.PathValues.Add(pathValue);

    private void Mutate(Action mutation)
    {
        mutation();
        this.HasCustomTemplate = true;
    }
}
