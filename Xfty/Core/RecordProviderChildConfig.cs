using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The child collections one <see cref="RecordProvider"/> call generates -
/// downward generation, the mirror of the usual upward ancestor generation.
/// Extracted so RecordProvider itself stays a thin fluent facade.
/// </summary>
internal sealed class RecordProviderChildConfig
{
    private readonly List<ChildProvider> childProviders = [];

    public void Add(ChildProvider childProvider) =>
        this.childProviders.Add(childProvider ?? throw new XftyConfigurationException("With(...) needs a ChildProvider."));

    public bool HasAny => this.childProviders.Count > 0;

    public Task GenerateAll(Bundle bundle, bool structural, RecordProviderExecutionState state) =>
        this.HasAny
            ? GenerateRemainingCollections(bundle, this.childProviders, structural, state)
            : Task.CompletedTask;

    private static async Task GenerateRemainingCollections(
        Bundle bundle, List<ChildProvider> childProviders, bool structural, RecordProviderExecutionState state)
    {
        if (childProviders.Count == 0)
        {
            return;
        }

        await GenerateOneCollection(bundle, childProviders[0], structural, state);
        await GenerateRemainingCollections(bundle, childProviders.Skip(1).ToList(), structural, state);
    }

    private static async Task GenerateOneCollection(Bundle bundle, ChildProvider childProvider, bool structural, RecordProviderExecutionState state)
    {
        PropertyInfo primaryField = state.FactoryOutlet.PrimaryTargetField;
        List<(object Template, int ParentRow)> childRows = ChildRowsFor(bundle, primaryField, childProvider, structural);
        RecordProvider childInstance = BuildChildInstance(childProvider, structural, childRows, state);
        Bundle childBundle = await childInstance.SupplyBundle();
        _ = bundle.PutChild(childProvider.RelationshipField, childBundle, [.. childRows.Select(row => row.ParentRow)]);
    }

    private static List<(object Template, int ParentRow)> ChildRowsFor(
        Bundle bundle, PropertyInfo primaryField, ChildProvider childProvider, bool structural)
    {
        List<object> primaries = bundle.GetList(primaryField)!;
        return [.. primaries
            .SelectMany((primary, parentRow) => childProvider
                .TemplatesForParent(structural ? null : IdOf(primary))
                .Select(template => (Template: template, ParentRow: parentRow)))];
    }

    private static RecordProvider BuildChildInstance(
        ChildProvider childProvider, bool structural, List<(object Template, int ParentRow)> childRows, RecordProviderExecutionState state)
    {
        InsertMode childMode = structural ? InsertMode.Never : childProvider.EffectiveInsertMode(state.InsertMode);
        RecordProvider childInstance = childProvider.NewProvider(state.ProviderLookup)
            .SetOverrideTemplateList([.. childRows.Select(row => row.Template)])
            .SetInsertMode(childMode)
            .SetInclusivity(childProvider.EffectiveInclusivity(state.Inclusivity));
        if (state.PersistenceGateway is not null)
        {
            _ = childInstance.SetPersistenceGateway(state.PersistenceGateway);
        }

        return structural
            // The back-reference is wired by the deferred buffer at flush, so the child must not also
            // generate its own parent on that field, and its own children stay structural for the same flush.
            ? childInstance.ExcludeRelationshipIfPresent(childProvider.RelationshipField).ForceStructuralChildGeneration()
            : childInstance;
    }

    private static object? IdOf(object record) =>
        record.GetType().GetProperty("Id")?.GetValue(record);
}
