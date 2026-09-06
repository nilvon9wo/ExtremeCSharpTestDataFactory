using System.Reflection;
using global::AutoBogus;
using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.AutoBogus;

/// <summary>
/// The AutoBogus-backed <see cref="IUnsetFieldFiller"/>: resolves each field
/// XFTY's Master Template never configured through the given
/// <see cref="IAutoFaker"/>, via the same generation pipeline
/// <c>faker.Generate&lt;T&gt;()</c> itself uses - so the faker's own
/// customizations (including <see cref="XftyAutoBogusOverride"/>, if also
/// installed on the same faker) apply exactly as they would to any other
/// AutoBogus request.
///
/// Unlike AutoFixture's default <c>ThrowingRecursionBehavior</c>, AutoBogus
/// never throws for a field that circles back on its own type - it simply
/// stops recursing past a fixed depth, leaving the deepest level's own
/// further-nested fields at their type's default. So, unlike
/// <c>Xfty.AutoFixture</c>'s equivalent filler, there is no recursion
/// exception to catch here.
///
/// <see cref="IAutoFaker"/>'s public surface is generic-only (no
/// <c>Generate(Type)</c> overload) - resolving an arbitrary
/// <see cref="PropertyInfo.PropertyType"/> discovered at runtime needs one
/// reflective <c>MakeGenericMethod</c> call per field, cached per type after
/// the first use.
///
/// Excluded(...) opts specific fields out entirely - see
/// <c>Xfty.AutoFixture</c>'s <c>AutoFixtureUnsetFieldFiller</c> (and this
/// package's own README) for why a navigation-shaped property
/// (<c>Contact.Account</c>/<c>Account.Contacts</c>, populated by
/// <c>Bundle.Inject(...)</c>/<c>InjectAll</c> rather than by any Master
/// Template) is the case that comes up in practice.
/// </summary>
public sealed class AutoBogusUnsetFieldFiller(IAutoFaker faker) : IUnsetFieldFiller
{
    private static readonly MethodInfo GenerateOfT = typeof(IAutoFaker)
        .GetMethod(nameof(IAutoFaker.Generate), 1, [typeof(Action<IAutoGenerateConfigBuilder>)])!;

    private static readonly Action<IAutoGenerateConfigBuilder> NoConfiguration = static _ => { };

    private readonly HashSet<PropertyInfo> excludedFields = [];
    private readonly Dictionary<Type, MethodInfo> generateMethodByType = [];

    /// <summary>Opt field out of this filler entirely - it stays exactly as XFTY left it. Chainable.</summary>
    public AutoBogusUnsetFieldFiller Excluding(PropertyInfo field)
    {
        _ = this.excludedFields.Add(field);
        return this;
    }

    public void Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields)
    {
        foreach (PropertyInfo field in unsetFields)
        {
            if (!this.excludedFields.Contains(field))
            {
                field.SetValue(record, this.Generate(field.PropertyType));
            }
        }
    }

    private object? Generate(Type type)
    {
        if (!this.generateMethodByType.TryGetValue(type, out MethodInfo? method))
        {
            method = GenerateOfT.MakeGenericMethod(type);
            this.generateMethodByType[type] = method;
        }

        return method.Invoke(faker, [NoConfiguration]);
    }
}
