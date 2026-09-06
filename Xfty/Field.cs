using System.Linq.Expressions;
using System.Reflection;

using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty;

/// <summary>
/// Resolves one property on a record type to the <see cref="PropertyInfo"/>
/// every other type in this library keys its maps by. <c>Field.Of&lt;Account&gt;(x
/// =&gt; x.Name)</c> is the normal, strongly-typed way to name a property; the
/// string overload exists only for the rare case a property name is not known
/// at compile time.
/// </summary>
public static class Field
{
    public static PropertyInfo Of<TRecord>(Expression<Func<TRecord, object?>> selector) =>
        PropertyOf(selector.Body);

    public static PropertyInfo Of<TRecord>(string propertyName) =>
        typeof(TRecord).GetProperty(propertyName)
        ?? throw new XftyConfigurationException($"{typeof(TRecord).Name} has no field named '{propertyName}'.");

    private static PropertyInfo PropertyOf(Expression body) =>
        body switch
        {
            MemberExpression { Member: PropertyInfo property } => property,
            UnaryExpression { Operand: MemberExpression { Member: PropertyInfo property } } => property,
            _ => throw new XftyConfigurationException($"'{body}' is not a simple property access (x => x.Field)."),
        };
}
