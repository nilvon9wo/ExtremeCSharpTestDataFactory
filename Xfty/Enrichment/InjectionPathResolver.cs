using System.Collections;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.Enrichment;

/// <summary>
/// Turns the PropertyInfo hops of an injection path into the navigation
/// property the enricher grafts onto. A plain foreign-key-shaped property
/// carries no relationship-name metadata of its own, so this resolves one by
/// convention instead: a lookup field named "XId" grafts the ancestor onto a
/// sibling "X" property on the same record; a child collection grafts onto
/// whichever property on the parent type holds a collection of the child's
/// own type. Both are resolved once here and validated against the actual
/// properties present, throwing a clear error that names the bad hop.
/// </summary>
public static class InjectionPathResolver
{
    private const string IdSuffix = "Id";

    /// <summary>The ancestor navigation property for a lookup field - Contact.Account for Contact.AccountId.</summary>
    public static PropertyInfo ParentRelationshipField(PropertyInfo lookupField)
    {
        string name = lookupField.Name;
        if (!name.EndsWith(IdSuffix, StringComparison.Ordinal) || name.Length == IdSuffix.Length)
        {
            throw BadHop($"{DescribeOf(lookupField)} does not follow the <Name>Id lookup convention - it cannot be an ancestor hop.");
        }

        string relationshipName = name[..^IdSuffix.Length];
        Type declaringType = DeclaringTypeOf(lookupField);
        return declaringType.GetProperty(relationshipName)
            ?? throw BadHop($"{declaringType.Name} has no {relationshipName} property to graft an ancestor under.");
    }

    /// <summary>The child-collection navigation property on parentType matching childLookupField's own record type - Account.Contacts for Account + Contact.AccountId.</summary>
    public static PropertyInfo ChildRelationshipField(Type parentType, PropertyInfo childLookupField)
    {
        Type childType = DeclaringTypeOf(childLookupField);
        List<PropertyInfo> candidates = [.. parentType.GetProperties().Where(property => ElementTypeOf(property.PropertyType) == childType)];
        return candidates.Count switch
        {
            1 => candidates[0],
            0 => throw BadHop(
                $"{parentType.Name} has no collection property of {childType.Name} - it cannot be an injected "
                + $"subquery for {DescribeOf(childLookupField)}."),
            _ => throw BadHop(
                $"{parentType.Name} has {candidates.Count} collection properties of {childType.Name} - injection "
                + $"cannot tell which one {DescribeOf(childLookupField)} means."),
        };
    }

    private static Type? ElementTypeOf(Type propertyType) =>
        propertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propertyType)
            ? propertyType.GetGenericArguments().FirstOrDefault()
            : null;

    private static Type DeclaringTypeOf(PropertyInfo field) =>
        field.DeclaringType ?? throw BadHop($"{field.Name} has no declaring type.");

    private static string DescribeOf(PropertyInfo field) => $"{field.DeclaringType?.Name}.{field.Name}";

    private static XftyConfigurationException BadHop(string detail) => new($"Injection path: {detail}");
}
