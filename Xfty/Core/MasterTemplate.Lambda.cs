using System.Linq.Expressions;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>MasterTemplate - naming a field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
public sealed partial class MasterTemplate
{
    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IValueExpression valueTemplate) =>
        this.Put(Field.Of(field), valueTemplate);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IContextAwareExpression contextAwareExpression) =>
        this.Put(Field.Of(field), contextAwareExpression);

    /// <summary>An up-flowing value - resolved during the DEFERRED flush. Naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, IDeferredExpression deferredValue) =>
        this.Put(Field.Of(field), deferredValue);

    /// <summary>Put(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate Put<TRecord>(Expression<Func<TRecord, object?>> field, object? value) =>
        this.Put(Field.Of(field), value);

    /// <summary>PutRequired(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate PutRequired<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.PutRequired(Field.Of(field), relationshipTemplate);

    /// <summary>PutOptional(field, ...), naming field by lambda instead of Field.Of&lt;TRecord&gt;(...).</summary>
    public MasterTemplate PutOptional<TRecord>(Expression<Func<TRecord, object?>> field, IDefaultRelationship relationshipTemplate) =>
        this.PutOptional(Field.Of(field), relationshipTemplate);
}
