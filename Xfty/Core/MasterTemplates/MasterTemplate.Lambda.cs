using System.Linq.Expressions;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core.MasterTemplates;

public sealed partial class MasterTemplate
{
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IValueYielding valueTemplate) =>
        this.Put(Field.Of(field), valueTemplate);

    public MasterTemplate Put<TRecord>(
        Expression<Func<TRecord, object?>> field,
        IContextAware contextAwareExpression
    ) =>
        this.Put(Field.Of(field), contextAwareExpression);

    /// <summary>An up-flowing value - resolved during the DEFERRED flush.</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IDeferred deferredValue) =>
        this.Put(Field.Of(field), deferredValue);

    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, object? value) =>
        this.Put(Field.Of(field), value);

    public MasterTemplate PutRequired<TRecord>(
        Expression<Func<TRecord, object?>> field,
        IRelatable relationshipTemplate
    ) =>
        this.PutRequired(Field.Of(field), relationshipTemplate);

    public MasterTemplate PutOptional<TRecord>(
        Expression<Func<TRecord, object?>> field,
        IRelatable relationshipTemplate
    ) =>
        this.PutOptional(Field.Of(field), relationshipTemplate);
}