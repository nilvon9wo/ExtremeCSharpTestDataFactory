using System.Linq.Expressions;
using System.Reflection;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>RecordProvider - `Put&lt;TRecord&gt;(x => x.Field, value)`, naming a field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
public sealed partial class RecordProvider
{
    public RecordProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IValueExpression valueTemplate) =>
        this.Put(Field.Of(field), valueTemplate);

    public RecordProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IContextAwareExpression contextAwareExpression) =>
        this.Put(Field.Of(field), contextAwareExpression);

    public RecordProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, IDeferredExpression deferredValue) =>
        this.Put(Field.Of(field), deferredValue);

    public RecordProvider Put<TRecord>(Expression<Func<TRecord, object?>> field, object? value) =>
        this.Put(Field.Of(field), value);

    public RecordProvider PutRequired<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.PutRequired(Field.Of(field), relationshipTemplate);

    public RecordProvider PutOptional<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.PutOptional(Field.Of(field), relationshipTemplate);

    public RecordProvider RemoveFromMasterTemplate<TRecord>(Expression<Func<TRecord, object?>> field) =>
        this.RemoveFromMasterTemplate(Field.Of(field));

    public RecordProvider IncludeOptional<TRecord>(Expression<Func<TRecord, object?>> field) =>
        this.IncludeOptional(Field.Of(field));

    public RecordProvider ExcludeRelationship<TRecord>(Expression<Func<TRecord, object?>> field) =>
        this.ExcludeRelationship(Field.Of(field));

    public RecordProvider ExcludeRelationshipIfPresent<TRecord>(Expression<Func<TRecord, object?>> field) =>
        this.ExcludeRelationshipIfPresent(Field.Of(field));
}
