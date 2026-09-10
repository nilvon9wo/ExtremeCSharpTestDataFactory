using System.Linq.Expressions;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.PathValues;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>
/// The single recipe for one shared ancestor's record - what
/// <see cref="SharedAncestorResolver"/> builds from. Obtained by chaining
/// onto <see cref="SharedAncestor.Put(string,object)"/>; never constructed
/// directly by a test.
/// </summary>
public sealed class SharedAncestorProvider(SharedAncestor owner)
{
    private readonly SharedAncestor _owner = owner;
    private readonly List<SharedAncestorFieldValue> _valuePuts = [];
    private readonly List<SharedAncestorFieldValue> _requiredRelationships = [];
    private readonly List<SharedAncestorFieldValue> _optionalRelationships = [];
    private readonly List<List<PropertyInfo>> _forcedRelationshipPaths = [];
    private readonly List<PathValue> _pathValues = [];

    private object? _overrideTemplate;
    private ILookupKey? _explicitKey;
    private ILookupKey? _resolvedKey;
    private PropertyInfo? _relatedField;
    private InsertInclusivity? _inclusivity;

    // Configuration ---------------------------------------------------

    public SharedAncestorProvider WithTemplate(object? overrideTemplate)
    {
        this._owner.AssertUnresolved("Put(name, template)");
        this._overrideTemplate = overrideTemplate;
        return this;
    }

    /// <summary>Pin the Provider variant that generates this shared record.</summary>
    public SharedAncestorProvider FromVariant(ILookupKey key)
    {
        this._owner.AssertUnresolved("FromVariant(...)");
        this._explicitKey = key;
        return this;
    }

    /// <summary>Copy this field from the shared record into the child's lookup, instead of its Id.</summary>
    public SharedAncestorProvider CopyingRelatedField(PropertyInfo relatedField)
    {
        this._owner.AssertUnresolved("CopyingRelatedField(...)");
        this._relatedField = relatedField;
        return this;
    }

    public SharedAncestorProvider CopyingRelatedField<TRecord>(Expression<Func<TRecord, object?>> relatedField) =>
        this.CopyingRelatedField(Field.Of(relatedField));

    /// <summary>Inclusivity for the shared record's own relationships (default Required).</summary>
    public SharedAncestorProvider SetInclusivity(InsertInclusivity inclusivity)
    {
        this._owner.AssertUnresolved("SetInclusivity(...)");
        this._inclusivity = inclusivity;
        return this;
    }

    public SharedAncestorProvider Put(PropertyInfo field, IValueExpression expression) =>
        this.AddValue(
            field,
            expression
        );

    public SharedAncestorProvider Put(PropertyInfo field, IContextAwareExpression expression) =>
        this.AddValue(
            field,
            expression
        );

    public SharedAncestorProvider Put(PropertyInfo field, object? literal) => this.AddValue(field, literal);

    public SharedAncestorProvider PutRequired(PropertyInfo field, IDefaultRelationship relationship)
    {
        this._owner.AssertUnresolved("PutRequired(...)");
        this._requiredRelationships.Add(new SharedAncestorFieldValue(field, relationship));
        return this;
    }

    public SharedAncestorProvider PutOptional(PropertyInfo field, IDefaultRelationship relationship)
    {
        this._owner.AssertUnresolved("PutOptional(...)");
        this._optionalRelationships.Add(new SharedAncestorFieldValue(field, relationship));
        return this;
    }

    public SharedAncestorProvider IncludeOptional(PropertyInfo relationshipField) =>
        this.IncludeOptional([relationshipField]);

    public SharedAncestorProvider IncludeOptional(List<PropertyInfo> relationshipPath)
    {
        this._owner.AssertUnresolved("IncludeOptional(...)");
        this._forcedRelationshipPaths.Add(relationshipPath);
        return this;
    }

    public SharedAncestorProvider IncludeOptional<TRecord>(Expression<Func<TRecord, object?>> relationshipField) =>
        this.IncludeOptional(Field.Of(relationshipField));

    public SharedAncestorProvider Put(List<PropertyInfo> path, IValueExpression expression) =>
        this.AddPathValue(PathValue.OfExpression(path, expression));

    public SharedAncestorProvider Put(List<PropertyInfo> path, IContextAwareExpression expression) =>
        this.AddPathValue(PathValue.OfContextAware(path, expression));

    public SharedAncestorProvider Put(List<PropertyInfo> path, object? literal) =>
        this.AddPathValue(PathValue.OfLiteral(path, literal));

    public SharedAncestorProvider PutRequired(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.AddPathValue(PathValue.OfRequiredRelationship(path, relationship));

    public SharedAncestorProvider PutOptional(List<PropertyInfo> path, IDefaultRelationship relationship) =>
        this.AddPathValue(PathValue.OfOptionalRelationship(path, relationship));

    // Lambda overloads (single field) - naming field by lambda instead of Field.Of<TRecord>(...) --------

    public SharedAncestorProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IValueExpression expression) =>
        this.Put(Field.Of(field), expression);

    public SharedAncestorProvider Put<TRecord>(
        Expression<Func<TRecord, object?>> field,
        IContextAwareExpression expression
    ) =>
        this.Put(Field.Of(field), expression);

    public SharedAncestorProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, object? literal) =>
        this.Put(Field.Of(field), literal);

    public SharedAncestorProvider PutRequired<TRecord>(
        Expression<Func<TRecord, object?>> field,
        IDefaultRelationship relationship
    ) =>
        this.PutRequired(Field.Of(field), relationship);

    public SharedAncestorProvider PutOptional<TRecord>(
        Expression<Func<TRecord, object?>> field,
        IDefaultRelationship relationship
    ) =>
        this.PutOptional(Field.Of(field), relationship);

    private SharedAncestorProvider AddValue(PropertyInfo field, object? value)
    {
        this._owner.AssertUnresolved("Put(...)");
        this._valuePuts.Add(new SharedAncestorFieldValue(field, value));
        return this;
    }

    private SharedAncestorProvider AddPathValue(PathValue pathValue)
    {
        this._owner.AssertUnresolved("Put(path, ...)");
        this._pathValues.Add(pathValue);
        return this;
    }

    // Used by SharedAncestor / SharedAncestorResolver -------------

    /// <summary>The field copied from the shared record into the child's lookup, or null.</summary>
    public PropertyInfo? RelatedField() => this._relatedField;

    /// <summary>The override template, if one was given - for IDefaultRelationship.</summary>
    public object? OverrideTemplate() => this._overrideTemplate;

    /// <summary>
    /// This ancestor's whole graph, generated with no persistence, ready for the depth-batched insert.
    /// </summary>
    public Task<Bundle> BuildInMemory(IProviderLookup lookup)
    {
        InsertInclusivity effectiveInclusivity = this._inclusivity ?? InsertInclusivity.Required;
        GenerationContext context = new GenerationContext(lookup, InsertMode.Never, effectiveInclusivity)
            .WithForcedRelationshipPaths(this._forcedRelationshipPaths)
            .WithPathValues(this._pathValues);
        object seed = RecordCloneFactory.DeepClone(this.RecordTemplate(lookup));
        return RecordFactory.CreateBundle(context, this.MasterTemplate(lookup), [seed]);
    }

    /// <summary>The primary target field of the shared record type.</summary>
    public PropertyInfo PrimaryField(IProviderLookup lookup) => this.BaseProvider(lookup).PrimaryTargetField;

    /// <summary>
    /// Whether <see cref="PrimaryField"/> can be resolved - it needs a template or a pinned variant to work from (a
    /// bare PutAsValue has neither).
    /// </summary>
    public bool CanResolvePrimaryField =>
        this._overrideTemplate is not null || this._explicitKey is not null || this._resolvedKey is not null;

    /// <summary>
    /// The Master Template the pre-phase scans for nested shared ancestors - with this ancestor's puts applied.
    /// </summary>
    public MasterTemplate MasterTemplate(IProviderLookup lookup)
    {
        MasterTemplate template = this.BaseProvider(lookup).MasterTemplate.Copy();
        this._valuePuts.ForEach(put => template.Put(put.Field, put.Value));
        this._requiredRelationships.ForEach(put => template.PutRequired(put.Field, (IDefaultRelationship)put.Value!));
        this._optionalRelationships.ForEach(put => template.PutOptional(put.Field, (IDefaultRelationship)put.Value!));
        return template;
    }

    /// <summary>True when the shared record is a single row with no sub-graph of its own.</summary>
    public bool IsLightweight(IProviderLookup lookup)
    {
        if (this._requiredRelationships.Count > 0
            || this._optionalRelationships.Count > 0
            || this._forcedRelationshipPaths.Count > 0)
        {
            return false;
        }

        if (this._pathValues.Any(pathValue => pathValue.IsRelationshipKind()))
        {
            return false;
        }

        MasterTemplate baseTemplate = this.BaseProvider(lookup).MasterTemplate;
        return baseTemplate.RequiredRelationshipByField.Count == 0
            && baseTemplate.OptionalRelationshipByField.Count == 0;
    }

    /// <summary>The lookup key this ancestor resolves under.</summary>
    public ILookupKey LookupKey(IProviderLookup lookup) =>
        this._resolvedKey ??= this._explicitKey ?? ProviderLookups.Resolve(lookup, this.RequireTemplate());

    // ---------------------------------------------------------------

    private IRecordProvider BaseProvider(IProviderLookup lookup) => lookup.Get(this.LookupKey(lookup));

    private object RecordTemplate(IProviderLookup lookup) =>
        this._overrideTemplate ?? BlankInstances.Of(this.LookupKey(lookup).RecordType);

    private object RequireTemplate() =>
        this._overrideTemplate ?? throw new XftyConfigurationException(
            $"Shared ancestor \"{this._owner.SharedName}\" needs "
            + "SharedAncestor.PutAsTemplate(...) or Put(name, key) before it can resolve."
        );
}