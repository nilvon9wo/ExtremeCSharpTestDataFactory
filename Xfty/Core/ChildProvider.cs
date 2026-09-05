using System.Linq.Expressions;
using System.Reflection;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;
namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// Configures one **child collection** hung off a <see cref="RecordProvider"/>
/// - the downward mirror of the framework's usual upward generation. The
/// child type is taken from the relationship field's declaring type, so
/// there is no type argument to keep in sync.
///
/// There is no metadata for "what type does this foreign-key-shaped property
/// conceptually reference" - a plain property only exposes its own declaring
/// type - so validating that a relationship field actually points at the
/// parent type it's hung off is not attempted. A misconfigured field surfaces
/// as a wrong/null value instead of failing fast at configuration time.
/// </summary>
public sealed class ChildProvider
{
    private readonly object template;
    private readonly List<ChildProviderPendingPut> pendingPuts = [];
    private readonly List<ChildProvider> grandchildProviders = [];

    private int quantity = 1;
    private InsertMode? insertModeOverride;
    private InsertInclusivity? inclusivityOverride;
    private ILookupKey? variantKey;

    public ChildProvider(PropertyInfo relationshipField) : this(relationshipField, null)
    {
    }

    public ChildProvider(PropertyInfo relationshipField, object? template)
    {
        this.RelationshipField = relationshipField ?? throw new XftyConfigurationException("ChildProvider needs the child relationship field.");
        this.ChildType = this.RelationshipField.DeclaringType!;
        if (template is not null && template.GetType() != this.ChildType)
        {
            throw new XftyConfigurationException($"Template is a {template.GetType()} but {relationshipField.Name} is on {this.ChildType}.");
        }

        this.template = template ?? Activator.CreateInstance(this.ChildType)!;
    }

    /// <summary>ChildProvider(field), naming field by lambda instead of Field.Of&lt;TChild&gt;(...).</summary>
    public static ChildProvider For<TChild>(Expression<Func<TChild, object?>> relationshipField) =>
        new(Field.Of(relationshipField));

    /// <summary>ChildProvider(field, template), naming field by lambda instead of Field.Of&lt;TChild&gt;(...).</summary>
    public static ChildProvider For<TChild>(Expression<Func<TChild, object?>> relationshipField, TChild template) =>
        new(Field.Of(relationshipField), template);

    public PropertyInfo RelationshipField { get; }

    public Type ChildType { get; }

    // Fluent config -------------------------------------------------------

    /// <summary>Children generated per primary. Default 1.</summary>
    public ChildProvider SetQuantity(int quantity)
    {
        this.quantity = quantity >= 1 ? quantity : throw new XftyConfigurationException($"SetQuantity({quantity}): at least 1.");
        return this;
    }

    public ChildProvider Put(PropertyInfo field, IValueExpression valueExpression) =>
        this.AddPendingPut(ChildProviderPendingPut.OfValue(field, valueExpression));

    public ChildProvider Put(PropertyInfo field, IContextAwareExpression contextAwareExpression) =>
        this.AddPendingPut(ChildProviderPendingPut.OfContextAware(field, contextAwareExpression));

    public ChildProvider Put(PropertyInfo field, object? literal) =>
        this.AddPendingPut(ChildProviderPendingPut.OfLiteral(field, literal));

    public ChildProvider PutRequired(PropertyInfo field, IDefaultRelationship relationship) =>
        this.AddPendingPut(ChildProviderPendingPut.OfRequiredRelationship(field, relationship));

    public ChildProvider PutOptional(PropertyInfo field, IDefaultRelationship relationship) =>
        this.AddPendingPut(ChildProviderPendingPut.OfOptionalRelationship(field, relationship));

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public ChildProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IValueExpression valueExpression) =>
        this.Put(Field.Of(field), valueExpression);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public ChildProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IContextAwareExpression contextAwareExpression) =>
        this.Put(Field.Of(field), contextAwareExpression);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public ChildProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, object? literal) =>
        this.Put(Field.Of(field), literal);

    /// <summary>PutRequired(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public ChildProvider PutRequired<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationship) =>
        this.PutRequired(Field.Of(field), relationship);

    /// <summary>PutOptional(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public ChildProvider PutOptional<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationship) =>
        this.PutOptional(Field.Of(field), relationship);

    private ChildProvider AddPendingPut(ChildProviderPendingPut pendingPut)
    {
        this.pendingPuts.Add(pendingPut);
        return this;
    }

    /// <summary>Insert mode for the children. Default: the parent Provider's. Cannot mix mock Ids with real DML.</summary>
    public ChildProvider SetInsertMode(InsertMode insertMode)
    {
        this.insertModeOverride = insertMode;
        return this;
    }

    /// <summary>Inclusivity for the children's *own* other relationships. Default: the parent Provider's.</summary>
    public ChildProvider SetInclusivity(InsertInclusivity inclusivity)
    {
        this.inclusivityOverride = inclusivity;
        return this;
    }

    /// <summary>Pin the child Provider variant (otherwise derived from the template's type).</summary>
    public ChildProvider WithVariant(ILookupKey variantKey)
    {
        this.variantKey = variantKey;
        return this;
    }

    /// <summary>Nest a further child collection under these children - grandchildren, and so on.</summary>
    public ChildProvider With(ChildProvider? grandchildProvider)
    {
        this.grandchildProviders.Add(grandchildProvider ?? throw new XftyConfigurationException("With(...) needs a ChildProvider."));
        return this;
    }

    // Used by RecordProvider -----------------------------------------------

    public InsertMode EffectiveInsertMode(InsertMode parentMode)
    {
        InsertMode mode = this.insertModeOverride ?? parentMode;
        AssertModesCompatible(parentMode, mode);
        return mode;
    }

    public InsertInclusivity EffectiveInclusivity(InsertInclusivity parentInclusivity) =>
        this.inclusivityOverride ?? parentInclusivity;

    /// <summary>Build the quantity child templates for one primary, back-reference set.</summary>
    public List<object> TemplatesForParent(object? parentId) =>
        [.. Enumerable.Range(0, this.quantity).Select(_ => this.CloneWithBackReference(parentId))];

    private object CloneWithBackReference(object? parentId)
    {
        object childTemplate = RecordCloneFactory.DeepClone(this.template);
        this.RelationshipField.SetValue(childTemplate, parentId);
        return childTemplate;
    }

    /// <summary>A fresh Provider for these children, with this child provider's puts/variant/nested children applied.</summary>
    public RecordProvider NewProvider(IProviderLookup lookup)
    {
        RecordProvider provider = this.variantKey is null
            ? new RecordProvider(this.ChildType, lookup)
            : new RecordProvider(this.variantKey, lookup);
        this.pendingPuts.ForEach(pendingPut => pendingPut.ApplyTo(provider));
        this.grandchildProviders.ForEach(grandchild => provider.With(grandchild));
        return provider;
    }

    private static void AssertModesCompatible(InsertMode parentMode, InsertMode childMode)
    {
        bool mixesMockWithReal = (parentMode, childMode) is (InsertMode.Mock, InsertMode.Now) or (InsertMode.Now, InsertMode.Mock);
        if (mixesMockWithReal)
        {
            throw new XftyConfigurationException(
                $"A child collection cannot mix mock Ids with real DML - parent is {parentMode}, child is {childMode}.");
        }
    }
}
