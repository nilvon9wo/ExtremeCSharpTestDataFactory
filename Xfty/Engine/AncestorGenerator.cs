using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;
namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>Generates the ancestor sub-bundle for each relationship the inclusivity covers.</summary>
public sealed class AncestorGenerator(GenerationContext context, int quantity, MasterTemplate template)
{
    private readonly GenerationContext _context = context;
    private readonly int _quantity = quantity;
    private readonly MasterTemplate _template = template;

    public async Task<Bundle> Generate()
    {
        Bundle bundle = new();
        HashSet<PropertyInfo> forcedHeads = this.ExplicitlyRequestedRelationshipHeads();
        await this.AddRemainingAncestors(bundle, this.RelationshipFields(), forcedHeads).ConfigureAwait(false);
        return bundle;
    }

    private async Task AddRemainingAncestors(Bundle bundle, List<PropertyInfo> fields, HashSet<PropertyInfo> forcedHeads)
    {
        if (fields.Count == 0)
        {
            return;
        }

        await this.AddAncestor(bundle, fields[0], forcedHeads.Contains(fields[0])).ConfigureAwait(false);
        await this.AddRemainingAncestors(bundle, [.. fields.Skip(1)], forcedHeads).ConfigureAwait(false);
    }

    private List<PropertyInfo> RelationshipFields()
    {
        HashSet<PropertyInfo> fields = this._context.Inclusivity == InsertInclusivity.None
            ? []
            : this.RequiredAndMaybeOptionalFields();

        // IncludeOptional(...) / Put(path, ...) name a relationship for THIS call -
        // generate it whatever the inclusivity says.
        fields.UnionWith(this.ExplicitlyRequestedRelationshipHeads());
        return [.. fields];
    }

    private HashSet<PropertyInfo> RequiredAndMaybeOptionalFields()
    {
        HashSet<PropertyInfo> fields = [.. this._template.RequiredRelationshipByField.Keys];
        if (this._context.Inclusivity == InsertInclusivity.All)
        {
            fields.UnionWith(this._template.OptionalRelationshipByField.Keys);
        }

        return fields;
    }

    private HashSet<PropertyInfo> ExplicitlyRequestedRelationshipHeads()
    {
        HashSet<PropertyInfo> heads = [.. this._context.ForcedRelationshipPaths
            .Where(path => path.Count > 0 && this.IsRelationshipHere(path[0]))
            .Select(path => path[0])];
        heads.UnionWith(this._context.PathValues
            .Where(pathValue => this.IsRelationshipHere(pathValue.Head())
                && (!pathValue.IsAtTarget() || pathValue.IsRelationshipKind()))
            .Select(pathValue => pathValue.Head()));
        return heads;
    }

    private bool IsRelationshipHere(PropertyInfo field) =>
        this._template.RequiredRelationshipByField.ContainsKey(field)
        || this._template.OptionalRelationshipByField.ContainsKey(field);

    private Task AddAncestor(Bundle bundle, PropertyInfo field, bool isForced)
    {
        IDefaultRelationship relationship = this.RelationshipOn(field)!;
        if (relationship is ISharedRelationship shared)
        {
            this.AssertNoPathValueInto(field);
            return this.WireSharedAncestor(bundle, field, shared);
        }

        return this.GenerateAncestor(bundle, field, relationship, isForced);
    }

    /// <summary>
    /// A Put(path, ...) that sets a plain value on a shared ancestor is
    /// rejected - the shared record is resolved once and shared by every
    /// child, so a per-call value has no well-defined meaning.
    /// </summary>
    private void AssertNoPathValueInto(PropertyInfo field)
    {
        bool setsAValueOnTheSharedRecord = this._context.PathValues
            .Where(pathValue => pathValue.Head() == field && !pathValue.IsSharedRelationshipValue())
            .Any(pathValue => !pathValue.IsAtTarget() || pathValue.IsRelationshipKind());
        if (setsAValueOnTheSharedRecord)
        {
            throw new XftyConfigurationException(
                $"Put(...) with a path through {field.Name} sets a value on a shared ancestor. Configure the "
                + "shared record with SharedAncestor.Put(name, ...) instead.");
        }
    }

    private Task WireSharedAncestor(Bundle bundle, PropertyInfo field, ISharedRelationship shared) =>
        new SharedRelationshipWiring(this._context, shared).Wire(bundle, field, this._quantity);

    private async Task GenerateAncestor(Bundle bundle, PropertyInfo field, IDefaultRelationship relationship, bool isForced)
    {
        ILookupKey childKey = relationship.ResolveLookupKey(this._context.ProviderLookup)!;
        this.AssertNoAncestorCycle(field, childKey);
        IRecordProvider provider = this._context.ProviderLookup.Get(childKey);
        GenerationContext childContext = ForcedChildContext(this._context.ForRelated(field), isForced)
            .EnteringProviderFor(childKey.HashKey);
        List<object> templates = ClonedTemplatesFor(relationship, this._quantity);
        Bundle generated = await provider.CreateBundle(childContext, templates).ConfigureAwait(false);
        List<object>? primaries = generated.GetList(provider.PrimaryTargetField);
        _ = bundle.Put(field, generated);
        _ = bundle.Put(field, primaries!);
    }

    /// <summary>
    /// An explicitly forced ancestor is generated **fully formed** - its own
    /// required relationships fill in - even when the surrounding call asked
    /// for NONE. Everything not on a forced path still follows the call's
    /// inclusivity.
    /// </summary>
    private static GenerationContext ForcedChildContext(GenerationContext childContext, bool isForced)
    {
        bool bumpNeeded = isForced && childContext.Inclusivity == InsertInclusivity.None;
        return bumpNeeded
            ? childContext.WithInclusivity(InsertInclusivity.Required)
            : childContext;
    }

    private void AssertNoAncestorCycle(PropertyInfo field, ILookupKey childKey)
    {
        if (!this._context.CycleGuard.WouldCycleOn(childKey.HashKey))
        {
            return;
        }

        throw new XftyConfigurationException(
            $"Relationship {field.Name} would generate another {childKey.RecordType}, but one is already being "
            + "generated further up this graph - a cycle. Use distinct per-level Providers (different lookup "
            + "keys), PreventCascade, or allow ancestor cycles when the chain terminates on its own.");
    }

    private static List<object> ClonedTemplatesFor(IDefaultRelationship relationship, int quantity) =>
        RecordCloneFactory.DeepClones(relationship.OverrideTemplate!, quantity);

    private IDefaultRelationship? RelationshipOn(PropertyInfo field) =>
        this._template.RequiredRelationshipByField.TryGetValue(field, out IDefaultRelationship? required)
            ? required
            : this._template.OptionalRelationshipByField.GetValueOrDefault(field);
}