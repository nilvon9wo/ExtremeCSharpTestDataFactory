using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// XFTY's definition of "this field is unset, so a value or relationship pass
/// may fill it": the field still holds the value a freshly-built instance
/// would have. For a reference type or <c>Nullable&lt;T&gt;</c> that is
/// <c>null</c>; for a non-nullable value type (<c>int</c>, <c>bool</c>,
/// <c>Guid</c>, an <c>enum</c>, <c>DateTime</c>) it is that type's default,
/// because C# gives those no <c>null</c> to test against.
///
/// The consequence, the same for value and reference types: an override
/// template cannot force a field to its empty value (0 / false / the empty
/// Guid, or null) and have that beat a Provider default - "set to empty" and
/// "never set" are indistinguishable here. Any other value is kept. The
/// tracked per-call <c>provider[x =&gt; x.Field] = value</c> form, or
/// <c>RemoveFromMasterTemplate(x =&gt; x.Field)</c>, does it when a test needs
/// the empty value pinned.
/// </summary>
internal static class FieldState
{
    public static bool IsUnset(PropertyInfo field, object record) =>
        Equals(field.GetValue(record), FreshValueOf(field.PropertyType));

    private static object? FreshValueOf(Type fieldType)
    {
        bool isNonNullableValueType = fieldType.IsValueType && Nullable.GetUnderlyingType(fieldType) is null;
        return isNonNullableValueType
            ? Activator.CreateInstance(fieldType)
            : null;
    }
}
