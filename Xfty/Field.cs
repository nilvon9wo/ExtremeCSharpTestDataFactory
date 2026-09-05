using Net.Nowhereatall.Xfty.Core;
using System.Reflection;

namespace Net.Nowhereatall.Xfty;

/// <summary>
/// Resolves a record type's field the way Apex's compiler-checked
/// <c>Account.Industry</c> token does, but via reflection - C# has no
/// built-in field-token literal, so this is the direct equivalent: a
/// <see cref="PropertyInfo"/>, obtained once by name.
/// </summary>
public static class Field
{
    public static PropertyInfo Of<TRecord>(string propertyName) =>
        typeof(TRecord).GetProperty(propertyName)
        ?? throw new XftyConfigurationException($"{typeof(TRecord).Name} has no field named '{propertyName}'.");
}
