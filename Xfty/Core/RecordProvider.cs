using System.Reflection;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// The primary entry point for the port: configure a record's fields and
/// relationships, then Supply()/SupplyList()/SupplyBundle() it.
///
/// Not yet ported onto this class: child collections (With/WithChildren/
/// WithChild - need SObjectChildProvider), depth-batched insert, the
/// DEFERRED registry, and shared-ancestor resolution - all wait on machinery
/// not yet in this port (see csharp-port-idea.md). The core single-record/
/// ancestor-generation path is fully wired.
/// </summary>
public sealed class RecordProvider
{
    private readonly Type sObjectType;
    private readonly IProviderLookup providerLookup;
    private readonly List<List<PropertyInfo>> forcedRelationshipPaths = [];
    private readonly List<PathValue> pathValues = [];

    private List<object>? overrideTemplateList;
    private ILookupKey? explicitVariantKey;
    private int quantityPerListedTemplate = 1;
    private InsertMode insertMode = InsertMode.Never;
    private InsertInclusivity inclusivity = InsertInclusivity.None;
    private bool hasCustomMasterTemplate;
    private bool ancestorCyclesAllowed;
    private bool depthBatched;
    private bool forceStructuralChildGeneration;
    private IRecordProvider? _factoryOutlet { get; set; }

    private MasterTemplate? _template { get; set; }

    public RecordProvider(Type sObjectType, IProviderLookup providerLookup)
    {
        this.sObjectType = sObjectType ?? throw new XftyConfigurationException("A record type is required to request data.");
        this.providerLookup = providerLookup ?? throw new XftyConfigurationException("A Provider Lookup is required to request data.");
    }

    /// <summary>Convenience: start from a lookup key. The record type is taken from the key, pinned as the variant.</summary>
    public RecordProvider(ILookupKey variantKey, IProviderLookup providerLookup)
        : this(TypeOf(variantKey), providerLookup) =>
        this.explicitVariantKey = variantKey;

    /// <summary>Convenience: start from an override template. The record type is taken from the template.</summary>
    public RecordProvider(object overrideTemplate, IProviderLookup providerLookup)
        : this([overrideTemplate], providerLookup)
    {
    }

    /// <summary>Convenience: start from a list of override templates. The record type is taken from the first template.</summary>
    public RecordProvider(List<object> overrideTemplateList, IProviderLookup providerLookup)
        : this(TypeOf(overrideTemplateList), providerLookup) =>
        this.SetOverrideTemplateList(overrideTemplateList);

    private static Type TypeOf(ILookupKey variantKey) =>
        (variantKey ?? throw new XftyConfigurationException("A lookup key is required to request data.")).SObjectType;

    private static Type TypeOf(List<object>? overrideTemplateList) =>
        overrideTemplateList is { Count: > 0 } && overrideTemplateList[0] is not null
            ? overrideTemplateList[0].GetType()
            : throw new XftyConfigurationException(
                "Cannot derive a record type from an empty or null template list - supply at least one concrete "
                + "template, or use the (Type, lookup) constructor.");

    private IRecordProvider FactoryOutlet => this._factoryOutlet ??= this.providerLookup.Get(this.ResolveVariantKey());

    /// <summary>
    /// Which Provider variant to use: an explicit key from WithVariant(...),
    /// or the key derived from the first override template, or the plain
    /// record-type key. Only consulted the first time the Provider is
    /// resolved.
    /// </summary>
    private ILookupKey ResolveVariantKey()
    {
        object? firstTemplate = this.overrideTemplateList is { Count: > 0 } ? this.overrideTemplateList[0] : null;
        return ProviderLookups.Reconcile(this.providerLookup, this.explicitVariantKey, firstTemplate)
            ?? LookupKey.Get(this.sObjectType);
    }

    private MasterTemplate Template => this._template ??= this.FactoryOutlet.MasterTemplate.Copy();

    // Setters ---------------------------------------------------------

    public RecordProvider SetQuantityPerTemplate(int quantityPerListedTemplate)
    {
        this.quantityPerListedTemplate = quantityPerListedTemplate >= 1
            ? quantityPerListedTemplate
            : throw new XftyConfigurationException($"It makes no sense to supply {quantityPerListedTemplate}.");
        return this;
    }

    /// <summary>The record type set by the constructor always wins - a list of a different type throws.</summary>
    public RecordProvider SetOverrideTemplateList(List<object> overrideTemplateList)
    {
        this.AssertNoSObjectTypeConflict(overrideTemplateList);
        this.overrideTemplateList = overrideTemplateList;
        return this;
    }

    public RecordProvider SetOverrideTemplate(object overrideTemplate) =>
        this.SetOverrideTemplateList([overrideTemplate]);

    /// <summary>Pin the Provider variant explicitly, instead of letting it be derived from the override template.</summary>
    public RecordProvider WithVariant(ILookupKey variantKey)
    {
        if (this.hasCustomMasterTemplate)
        {
            throw new XftyConfigurationException("Call WithVariant(...) before customizing the template with Put(...).");
        }

        this.AssertVariantKeyMatchesType(variantKey);
        this.explicitVariantKey = variantKey;
        return this;
    }

    private void AssertVariantKeyMatchesType(ILookupKey? variantKey)
    {
        ILookupKey key = variantKey ?? throw new XftyConfigurationException("A variant key is required.");
        if (key.SObjectType != this.sObjectType)
        {
            throw new RecordProviderConflictException(
                $"Variant key is for {key.SObjectType} but this Provider requests {this.sObjectType}.");
        }
    }

    public RecordProvider SetInsertMode(InsertMode insertMode)
    {
        this.insertMode = insertMode;
        return this;
    }

    public RecordProvider SetInclusivity(InsertInclusivity inclusivity)
    {
        this.inclusivity = inclusivity;
        return this;
    }

    /// <summary>Suppress the ancestor-cycle guard for this call. Use only when the chain genuinely terminates on its own.</summary>
    public RecordProvider AllowAncestorCycles()
    {
        this.ancestorCyclesAllowed = true;
        return this;
    }

    /// <summary>
    /// Opt in to one insert per dependency depth for this Now call, instead
    /// of one per Provider. Not supported with shared ancestors resolved
    /// under manual mode.
    /// </summary>
    public RecordProvider DepthBatched()
    {
        this.depthBatched = true;
        return this;
    }

    /// <summary>Internal: a child of a DEFERRED/depth-batched parent must build its own children structurally too.</summary>
    public RecordProvider ForceStructuralChildGeneration()
    {
        this.forceStructuralChildGeneration = true;
        return this;
    }

    // Include methods ---------------------------------------------------

    public RecordProvider Put(PropertyInfo field, IValueExpression valueTemplate) =>
        this.PutOnTemplate(() => this.Template.Put(field, valueTemplate));

    public RecordProvider Put(PropertyInfo field, IContextAwareExpression contextAwareExpression) =>
        this.PutOnTemplate(() => this.Template.Put(field, contextAwareExpression));

    /// <summary>An up-flowing value; needs the DEFERRED insert mode.</summary>
    public RecordProvider Put(PropertyInfo field, IDeferredExpression deferredValue) =>
        this.PutOnTemplate(() => this.Template.Put(field, deferredValue));

    public RecordProvider PutRequired(PropertyInfo field, IDefaultRelationship relationshipTemplate) =>
        this.PutOnTemplate(() => this.Template.Remove(field).PutRequired(field, relationshipTemplate));

    public RecordProvider PutOptional(PropertyInfo field, IDefaultRelationship relationshipTemplate) =>
        this.PutOnTemplate(() => this.Template.Remove(field).PutOptional(field, relationshipTemplate));

    /// <summary>Convenience overload mirroring MasterTemplate.Put(field, object): routed by runtime type.</summary>
    public RecordProvider Put(PropertyInfo field, object? value) =>
        value switch
        {
            IDeferredExpression deferred => this.Put(field, deferred),
            IContextAwareExpression contextAware => this.Put(field, contextAware),
            IValueExpression valueExpression => this.Put(field, valueExpression),
            IDefaultRelationship => throw new XftyConfigurationException(
                "Relationships must be added with PutRequired(...) or PutOptional(...), not Put(...)."),
            _ => this.Put(field, new LiteralExpression(value)),
        };

    public RecordProvider RemoveFromMasterTemplate(PropertyInfo field) =>
        this.PutOnTemplate(() => this.Template.Remove(field));

    private RecordProvider PutOnTemplate(Action mutation)
    {
        mutation();
        this.hasCustomMasterTemplate = true;
        return this;
    }

    // Per-call relationship control ---------------------------------

    /// <summary>Generate one specific relationship on this call, on top of whatever SetInclusivity(...) covers.</summary>
    public RecordProvider IncludeOptional(PropertyInfo field) =>
        this.IncludeOptional([field]);

    /// <summary>Reach down the graph: force every relationship along the path for this call.</summary>
    public RecordProvider IncludeOptional(List<PropertyInfo> relationshipPath)
    {
        if (relationshipPath is not { Count: > 0 } || relationshipPath.Any(step => step is null))
        {
            throw new XftyConfigurationException("IncludeOptional(...) needs at least one non-null relationship field.");
        }

        this.forcedRelationshipPaths.Add(relationshipPath);
        return this;
    }

    // Path-scoped value overrides -------------------------------------

    public RecordProvider Put(List<PropertyInfo> path, IValueExpression valueExpression) =>
        this.AddPathValue(PathValue.OfExpression(path, valueExpression));

    public RecordProvider Put(List<PropertyInfo> path, IContextAwareExpression contextAwareExpression) =>
        this.AddPathValue(PathValue.OfContextAware(path, contextAwareExpression));

    public RecordProvider Put(List<PropertyInfo> path, object? literal) =>
        this.AddPathValue(PathValue.OfLiteral(path, literal));

    public RecordProvider PutRequired(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.AddPathValue(PathValue.OfRequiredRelationship(path, relationship));

    public RecordProvider PutOptional(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.AddPathValue(PathValue.OfOptionalRelationship(path, relationship));

    private RecordProvider AddPathValue(PathValue pathValue)
    {
        this.pathValues.Add(pathValue);
        return this;
    }

    /// <summary>Do not generate one specific relationship on this call - required or optional.</summary>
    public RecordProvider ExcludeRelationship(PropertyInfo field) =>
        this.IsRelationshipOnTemplate(field)
            ? this.PutOnTemplate(() => this.Template.Remove(field))
            : throw new XftyConfigurationException($"ExcludeRelationship({field.Name}): {this.sObjectType} has no relationship on that field.");

    /// <summary>Like ExcludeRelationship, but a no-op when the field is not a relationship on this Provider.</summary>
    public RecordProvider ExcludeRelationshipIfPresent(PropertyInfo field) =>
        this.IsRelationshipOnTemplate(field)
            ? this.PutOnTemplate(() => this.Template.Remove(field))
            : this;

    private bool IsRelationshipOnTemplate(PropertyInfo field) =>
        this.Template.RequiredRelationshipByField.ContainsKey(field) || this.Template.OptionalRelationshipByField.ContainsKey(field);

    // Supply methods ----------------------------------------------------

    public Bundle SupplyBundle()
    {
        this.WarnIfMixingCustomTemplateWithOverrides();
        SharedAncestorResolver.ResolveAllConfigured(this.providerLookup, this.insertMode);
        GenerationContext context = this.BuildContext();
        List<object> templates = this.TemplatesToFill();
        Bundle bundle = this.Generate(context, templates);
        this.SupplyChildrenAndPersist(bundle);
        return bundle;
    }

    private void SupplyChildrenAndPersist(Bundle bundle)
    {
        if (this.BuildsStructurallyForBatchedInsert())
        {
            // Children join the same deferred graph - generated structurally now, FK wired when the buffer flushes.
            this.GenerateChildren(bundle, structural: true);
            this.Persist(bundle);
        }
        else if (this.forceStructuralChildGeneration)
        {
            // A structural child of a deferred parent: nothing to persist here, but its own children stay structural too.
            this.GenerateChildren(bundle, structural: true);
        }
        else
        {
            // Now/Mock: primaries already have Ids after Generate(); wire the children's back-reference concretely.
            this.GenerateChildren(bundle, structural: false);
        }
    }

    public List<object> SupplyList() =>
        this.SupplyBundle().GetList(this.FactoryOutlet.PrimaryTargetField)!;

    public object Supply() => this.SupplyList()[0];

    private Bundle Generate(GenerationContext context, List<object> templates) =>
        this.hasCustomMasterTemplate
            ? RecordFactory.CreateBundle(context, this.Template, templates)
            : this.FactoryOutlet.CreateBundle(context, templates);

    private void Persist(Bundle bundle)
    {
        if (this.FlushesGraphWhenThisCallEnds())
        {
            DeferredInsertBuffer.InsertGraph(bundle);
        }
        else if (this.DeferredToRegistry())
        {
            DeferredInserter.Register(bundle);
        }
    }

    // Downward generation - child collections ----------------------------

    private readonly List<ChildProvider> childProviders = [];

    /// <summary>Add a fully-configured child collection. Repeatable.</summary>
    public RecordProvider With(ChildProvider childProvider)
    {
        this.childProviders.Add(childProvider ?? throw new XftyConfigurationException("With(...) needs a ChildProvider."));
        return this;
    }

    /// <summary>Shortcut: countPerParent children on childRelationshipField, everything else defaulted.</summary>
    public RecordProvider WithChildren(PropertyInfo childRelationshipField, int countPerParent) =>
        this.With(new ChildProvider(childRelationshipField).SetQuantity(countPerParent));

    /// <summary>Shortcut: one child on childRelationshipField.</summary>
    public RecordProvider WithChild(PropertyInfo childRelationshipField) =>
        this.With(new ChildProvider(childRelationshipField));

    private void GenerateChildren(Bundle bundle, bool structural)
    {
        if (this.childProviders.Count == 0)
        {
            return;
        }

        this.childProviders.ForEach(childProvider => this.GenerateOneChildCollection(bundle, childProvider, structural));
    }

    private void GenerateOneChildCollection(Bundle bundle, ChildProvider childProvider, bool structural)
    {
        List<object> primaries = bundle.GetList(this.FactoryOutlet.PrimaryTargetField)!;
        List<(object Template, int ParentRow)> childRows = primaries
            .SelectMany((primary, parentRow) => childProvider
                .TemplatesForParent(structural ? null : IdOf(primary))
                .Select(template => (Template: template, ParentRow: parentRow)))
            .ToList();

        InsertMode childMode = structural ? InsertMode.Never : childProvider.EffectiveInsertMode(this.insertMode);
        RecordProvider childInstance = childProvider.NewProvider(this.providerLookup)
            .SetOverrideTemplateList(childRows.Select(row => row.Template).ToList())
            .SetInsertMode(childMode)
            .SetInclusivity(childProvider.EffectiveInclusivity(this.inclusivity));
        if (structural)
        {
            // The back-reference is wired by the deferred buffer at flush, so the child must not also
            // generate its own parent on that field, and its own children stay structural for the same flush.
            _ = childInstance.ExcludeRelationshipIfPresent(childProvider.RelationshipField).ForceStructuralChildGeneration();
        }

        Bundle childBundle = childInstance.SupplyBundle();
        _ = bundle.PutChild(childProvider.RelationshipField, childBundle, childRows.Select(row => row.ParentRow).ToList());
    }

    private static object? IdOf(object record) =>
        record.GetType().GetProperty("Id")?.GetValue(record);

    private GenerationContext BuildContext()
    {
        GenerationContext context = new GenerationContext(this.providerLookup, this.ContextInsertMode(), this.inclusivity)
            .WithForcedRelationshipPaths(this.forcedRelationshipPaths)
            .WithPathValues(this.pathValues)
            .WithAncestorCycleGuard(this.ancestorCyclesAllowed);
        return this.BuildsStructurallyForBatchedInsert()
            ? context.ForBatchedInsert()
            : context;
    }

    private InsertMode ContextInsertMode() =>
        this.BuildsStructurallyForBatchedInsert()
            ? InsertMode.Never
            : this.insertMode;

    private bool BuildsStructurallyForBatchedInsert() => this.FlushesGraphWhenThisCallEnds() || this.DeferredToRegistry();

    private bool FlushesGraphWhenThisCallEnds() => this.depthBatched && this.insertMode == InsertMode.Now;

    private bool DeferredToRegistry() => this.insertMode == InsertMode.Deferred;

    private List<object> TemplatesToFill()
    {
        List<object> templates = this.SuppliedOrBlankTemplates();
        return this.quantityPerListedTemplate > 1
            ? MultiplyByQuantity(templates, this.quantityPerListedTemplate)
            : templates;
    }

    private List<object> SuppliedOrBlankTemplates() =>
        this.HasOverrideTemplates()
            ? this.overrideTemplateList!
            : [Activator.CreateInstance(this.sObjectType)!];

    private bool HasOverrideTemplates() => this.overrideTemplateList is { Count: > 0 };

    private void WarnIfMixingCustomTemplateWithOverrides()
    {
        if (this.hasCustomMasterTemplate && this.HasOverrideTemplates())
        {
            Console.Error.WriteLine("Custom master template + overrides: overrides win all conflicts!");
        }
    }

    private static List<object> MultiplyByQuantity(List<object> templateList, int quantity) =>
        Enumerable.Range(1, quantity).SelectMany(_ => templateList).ToList();

    // Consistency checks --------------------------------------------

    private void AssertNoSObjectTypeConflict(List<object>? overrideTemplateList)
    {
        object? conflicting = overrideTemplateList?.FirstOrDefault(overrideTemplate => overrideTemplate.GetType() != this.sObjectType);
        if (conflicting is not null)
        {
            throw new RecordProviderConflictException(
                $"This Provider requests {this.sObjectType} but was given a {conflicting.GetType()} override template.");
        }
    }
}
