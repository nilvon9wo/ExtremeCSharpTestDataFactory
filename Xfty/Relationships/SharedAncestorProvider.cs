using System.Linq.Expressions;
using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Relationships;

/// <summary>
/// The single recipe for one shared ancestor's record - what
/// <see cref="SharedAncestorResolver"/> builds from. Obtained by chaining
/// onto <see cref="SharedAncestor.Put(string,object)"/>; never constructed
/// directly by a test.
/// </summary>
public sealed class SharedAncestorProvider
{
    private readonly SharedAncestor owner;
    private readonly List<SharedAncestorFieldValue> valuePuts = [];
    private readonly List<SharedAncestorFieldValue> requiredRelationships = [];
    private readonly List<SharedAncestorFieldValue> optionalRelationships = [];
    private readonly List<List<PropertyInfo>> forcedRelationshipPaths = [];
    private readonly List<PathValue> pathValues = [];

    private object? overrideTemplate;
    private ILookupKey? explicitKey;
    private ILookupKey? resolvedKey;
    private PropertyInfo? relatedField;
    private InsertInclusivity? inclusivity;

    public SharedAncestorProvider(SharedAncestor owner) => this.owner = owner;

    // Configuration ---------------------------------------------------

    public SharedAncestorProvider WithTemplate(object? overrideTemplate)
    {
        this.owner.AssertUnresolved("Put(name, template)");
        this.overrideTemplate = overrideTemplate;
        return this;
    }

    /// <summary>Pin the Provider variant that generates this shared record.</summary>
    public SharedAncestorProvider FromVariant(ILookupKey key)
    {
        this.owner.AssertUnresolved("FromVariant(...)");
        this.explicitKey = key;
        return this;
    }

    /// <summary>Copy this field from the shared record into the child's lookup, instead of its Id.</summary>
    public SharedAncestorProvider CopyingRelatedField(PropertyInfo relatedField)
    {
        this.owner.AssertUnresolved("CopyingRelatedField(...)");
        this.relatedField = relatedField;
        return this;
    }

    /// <summary>CopyingRelatedField(field), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public SharedAncestorProvider CopyingRelatedField<TRecord>(Expression<Func<TRecord, object?>> relatedField) =>
        this.CopyingRelatedField(Field.Of(relatedField));

    /// <summary>Inclusivity for the shared record's own relationships (default Required).</summary>
    public SharedAncestorProvider SetInclusivity(InsertInclusivity inclusivity)
    {
        this.owner.AssertUnresolved("SetInclusivity(...)");
        this.inclusivity = inclusivity;
        return this;
    }

    public SharedAncestorProvider Put(PropertyInfo field, IValueExpression expression) => this.AddValue(field, expression);

    public SharedAncestorProvider Put(PropertyInfo field, IContextAwareExpression expression) => this.AddValue(field, expression);

    public SharedAncestorProvider Put(PropertyInfo field, object? literal) => this.AddValue(field, literal);

    public SharedAncestorProvider PutRequired(PropertyInfo field, IDefaultRelationship relationship)
    {
        this.owner.AssertUnresolved("PutRequired(...)");
        this.requiredRelationships.Add(new SharedAncestorFieldValue(field, relationship));
        return this;
    }

    public SharedAncestorProvider PutOptional(PropertyInfo field, IDefaultRelationship relationship)
    {
        this.owner.AssertUnresolved("PutOptional(...)");
        this.optionalRelationships.Add(new SharedAncestorFieldValue(field, relationship));
        return this;
    }

    public SharedAncestorProvider IncludeOptional(PropertyInfo relationshipField) => this.IncludeOptional([relationshipField]);

    public SharedAncestorProvider IncludeOptional(List<PropertyInfo> relationshipPath)
    {
        this.owner.AssertUnresolved("IncludeOptional(...)");
        this.forcedRelationshipPaths.Add(relationshipPath);
        return this;
    }

    /// <summary>IncludeOptional(field), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
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

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public SharedAncestorProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IValueExpression expression) =>
        this.Put(Field.Of(field), expression);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public SharedAncestorProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IContextAwareExpression expression) =>
        this.Put(Field.Of(field), expression);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public SharedAncestorProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, object? literal) =>
        this.Put(Field.Of(field), literal);

    /// <summary>PutRequired(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public SharedAncestorProvider PutRequired<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationship) =>
        this.PutRequired(Field.Of(field), relationship);

    /// <summary>PutOptional(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public SharedAncestorProvider PutOptional<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationship) =>
        this.PutOptional(Field.Of(field), relationship);

    private SharedAncestorProvider AddValue(PropertyInfo field, object? value)
    {
        this.owner.AssertUnresolved("Put(...)");
        this.valuePuts.Add(new SharedAncestorFieldValue(field, value));
        return this;
    }

    private SharedAncestorProvider AddPathValue(PathValue pathValue)
    {
        this.owner.AssertUnresolved("Put(path, ...)");
        this.pathValues.Add(pathValue);
        return this;
    }

    // Used by SharedAncestor / SharedAncestorResolver -------------

    /// <summary>The field copied from the shared record into the child's lookup, or null.</summary>
    public PropertyInfo? RelatedField() => this.relatedField;

    /// <summary>The override template, if one was given - for IDefaultRelationship.</summary>
    public object? OverrideTemplate() => this.overrideTemplate;

    /// <summary>This ancestor's whole graph, generated with no persistence, ready for the depth-batched insert.</summary>
    public Bundle BuildInMemory(IProviderLookup lookup)
    {
        InsertInclusivity effectiveInclusivity = this.inclusivity ?? InsertInclusivity.Required;
        GenerationContext context = new GenerationContext(lookup, InsertMode.Never, effectiveInclusivity)
            .WithForcedRelationshipPaths(this.forcedRelationshipPaths)
            .WithPathValues(this.pathValues);
        object seed = RecordCloneFactory.DeepClone(this.RecordTemplate(lookup));
        return RecordFactory.CreateBundle(context, this.MasterTemplate(lookup), [seed]);
    }

    /// <summary>The primary target field (the record's Id field).</summary>
    public PropertyInfo PrimaryField(IProviderLookup lookup) => this.BaseProvider(lookup).PrimaryTargetField;

    /// <summary>The Master Template the pre-phase scans for nested shared ancestors - with this ancestor's puts applied.</summary>
    public MasterTemplate MasterTemplate(IProviderLookup lookup)
    {
        MasterTemplate template = this.BaseProvider(lookup).MasterTemplate.Copy();
        this.valuePuts.ForEach(put => template.Put(put.Field, put.Value));
        this.requiredRelationships.ForEach(put => template.PutRequired(put.Field, (IDefaultRelationship)put.Value!));
        this.optionalRelationships.ForEach(put => template.PutOptional(put.Field, (IDefaultRelationship)put.Value!));
        return template;
    }

    /// <summary>True when the shared record is a single row with no sub-graph of its own.</summary>
    public bool IsLightweight(IProviderLookup lookup)
    {
        if (this.requiredRelationships.Count > 0 || this.optionalRelationships.Count > 0 || this.forcedRelationshipPaths.Count > 0)
        {
            return false;
        }

        if (this.pathValues.Any(pathValue => pathValue.IsRelationshipKind()))
        {
            return false;
        }

        MasterTemplate baseTemplate = this.BaseProvider(lookup).MasterTemplate;
        return baseTemplate.RequiredRelationshipByField.Count == 0 && baseTemplate.OptionalRelationshipByField.Count == 0;
    }

    /// <summary>The lookup key this ancestor resolves under.</summary>
    public ILookupKey LookupKey(IProviderLookup lookup) =>
        this.resolvedKey ??= this.explicitKey ?? ProviderLookups.Resolve(lookup, this.RequireTemplate());

    // ---------------------------------------------------------------

    private IRecordProvider BaseProvider(IProviderLookup lookup) => lookup.Get(this.LookupKey(lookup));

    private object RecordTemplate(IProviderLookup lookup) =>
        this.overrideTemplate ?? Activator.CreateInstance(this.LookupKey(lookup).RecordType)!;

    private object RequireTemplate() =>
        this.overrideTemplate ?? throw new XftyConfigurationException(
            $"Shared ancestor \"{this.owner.SharedName}\" needs SharedAncestor.PutAsTemplate(...) or Put(name, key) before it can resolve.");
}
