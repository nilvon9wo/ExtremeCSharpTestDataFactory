using System.Reflection;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>Generates the ancestor sub-bundle for each relationship the inclusivity covers.</summary>
public sealed class AncestorGenerator
{
    private readonly GenerationContext context;
    private readonly int quantity;
    private readonly MasterTemplate template;

    public AncestorGenerator(GenerationContext context, int quantity, MasterTemplate template)
    {
        this.context = context;
        this.quantity = quantity;
        this.template = template;
    }

    public Bundle Generate()
    {
        Bundle bundle = new();
        HashSet<PropertyInfo> forcedHeads = this.ExplicitlyRequestedRelationshipHeads();
        this.RelationshipFields().ForEach(field => this.AddAncestor(bundle, field, forcedHeads.Contains(field)));
        return bundle;
    }

    private List<PropertyInfo> RelationshipFields()
    {
        HashSet<PropertyInfo> fields = this.context.Inclusivity == InsertInclusivity.None
            ? []
            : this.RequiredAndMaybeOptionalFields();

        // IncludeOptional(...) / Put(path, ...) name a relationship for THIS call -
        // generate it whatever the inclusivity says.
        fields.UnionWith(this.ExplicitlyRequestedRelationshipHeads());
        return [.. fields];
    }

    private HashSet<PropertyInfo> RequiredAndMaybeOptionalFields()
    {
        HashSet<PropertyInfo> fields = [.. this.template.RequiredRelationshipByField.Keys];
        if (this.context.Inclusivity == InsertInclusivity.All)
        {
            fields.UnionWith(this.template.OptionalRelationshipByField.Keys);
        }

        return fields;
    }

    private HashSet<PropertyInfo> ExplicitlyRequestedRelationshipHeads()
    {
        HashSet<PropertyInfo> heads = [.. this.context.ForcedRelationshipPaths
            .Where(path => path.Count > 0 && this.IsRelationshipHere(path[0]))
            .Select(path => path[0])];
        heads.UnionWith(this.context.PathValues
            .Where(pathValue => this.IsRelationshipHere(pathValue.Head())
                && (!pathValue.IsAtTarget() || pathValue.IsRelationshipKind()))
            .Select(pathValue => pathValue.Head()));
        return heads;
    }

    private bool IsRelationshipHere(PropertyInfo field) =>
        this.template.RequiredRelationshipByField.ContainsKey(field)
        || this.template.OptionalRelationshipByField.ContainsKey(field);

    private void AddAncestor(Bundle bundle, PropertyInfo field, bool isForced)
    {
        IDefaultRelationship relationship = this.RelationshipOn(field)!;
        if (relationship is ISharedRelationship shared)
        {
            this.AssertNoPathValueInto(field);
            this.WireSharedAncestor(bundle, field, shared);
            return;
        }

        this.GenerateAncestor(bundle, field, relationship, isForced);
    }

    /// <summary>
    /// A Put(path, ...) that sets a plain value on a shared ancestor is
    /// rejected - the shared record is resolved once and shared by every
    /// child, so a per-call value has no well-defined meaning.
    /// </summary>
    private void AssertNoPathValueInto(PropertyInfo field)
    {
        bool setsAValueOnTheSharedRecord = this.context.PathValues
            .Where(pathValue => pathValue.Head() == field && !pathValue.IsSharedRelationshipValue())
            .Any(pathValue => !pathValue.IsAtTarget() || pathValue.IsRelationshipKind());
        if (setsAValueOnTheSharedRecord)
        {
            throw new XftyConfigurationException(
                $"Put(...) with a path through {field.Name} sets a value on a shared ancestor. Configure the "
                + "shared record with SharedAncestor.Put(name, ...) instead.");
        }
    }

    private void WireSharedAncestor(Bundle bundle, PropertyInfo field, ISharedRelationship shared) =>
        new SharedRelationshipWiring(this.context, shared).Wire(bundle, field, this.quantity);

    private void GenerateAncestor(Bundle bundle, PropertyInfo field, IDefaultRelationship relationship, bool isForced)
    {
        ILookupKey childKey = relationship.ResolveLookupKey(this.context.ProviderLookup)!;
        this.AssertNoAncestorCycle(field, childKey);
        IRecordProvider provider = this.context.ProviderLookup.Get(childKey);
        GenerationContext childContext = this.ForcedChildContext(this.context.ForRelated(field), isForced)
            .EnteringProviderFor(childKey.HashKey);
        List<object> templates = ClonedTemplatesFor(relationship, this.quantity);
        Bundle generated = provider.CreateBundle(childContext, templates);
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
    private GenerationContext ForcedChildContext(GenerationContext childContext, bool isForced)
    {
        bool bumpNeeded = isForced && childContext.Inclusivity == InsertInclusivity.None;
        return bumpNeeded
            ? childContext.WithInclusivity(InsertInclusivity.Required)
            : childContext;
    }

    private void AssertNoAncestorCycle(PropertyInfo field, ILookupKey childKey)
    {
        if (!this.context.CycleGuard.WouldCycleOn(childKey.HashKey))
        {
            return;
        }

        throw new XftyConfigurationException(
            $"Relationship {field.Name} would generate another {childKey.SObjectType}, but one is already being "
            + "generated further up this graph - a cycle. Use distinct per-level Providers (different lookup "
            + "keys), PreventCascade, or allow ancestor cycles when the chain terminates on its own.");
    }

    private static List<object> ClonedTemplatesFor(IDefaultRelationship relationship, int quantity) =>
        RecordCloneFactory.DeepClones(relationship.OverrideTemplate!, quantity);

    private IDefaultRelationship? RelationshipOn(PropertyInfo field) =>
        this.template.RequiredRelationshipByField.TryGetValue(field, out IDefaultRelationship? required)
            ? required
            : this.template.OptionalRelationshipByField.GetValueOrDefault(field);
}
