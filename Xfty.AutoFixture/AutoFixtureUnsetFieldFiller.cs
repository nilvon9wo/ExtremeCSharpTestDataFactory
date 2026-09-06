using System.Reflection;
using global::AutoFixture;
using global::AutoFixture.Kernel;
using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.AutoFixture;

/// <summary>
/// The bundled <see cref="IUnsetFieldFiller"/>: resolves each field XFTY's
/// Master Template never configured through the given <see cref="IFixture"/>,
/// via the same specimen-builder pipeline <c>fixture.Create&lt;T&gt;()</c>
/// itself uses - so the fixture's own customizations, and its own recursion
/// guard, both apply exactly as they would to any other AutoFixture request.
///
/// A recursive/unsatisfiable field is skipped, not thrown for - the fixture's
/// default <c>ThrowingRecursionBehavior</c> raises <see cref="ObjectCreationException"/>
/// deep inside <see cref="ISpecimenContext.Resolve"/> for a field that
/// circles back on its own type (Fill never sees the whole record graph at
/// once, only one field at a time, so it cannot pre-detect this itself); this
/// filler catches that one exception type and leaves the field exactly as
/// XFTY left it. Install <c>fixture.Behaviors</c>'s <c>OmitOnRecursionBehavior</c>
/// in place of the default `Throwing` one for a fixture that already handles
/// this by returning <see cref="OmitSpecimen"/> instead of throwing at all.
///
/// Excluded(...) opts specific fields out entirely - most commonly a
/// navigation-shaped property (a bundle-only convenience like
/// <c>Contact.Account</c>/<c>Account.Contacts</c>, populated by
/// <c>Bundle.Inject(...)</c>/<c>InjectAll</c> rather than by any Master
/// Template) that this filler would otherwise dutifully hand a fake,
/// unrelated value neither the template nor an enrichment pass asked it for
/// - see this package's README for the fuller explanation of that gap.
/// </summary>
public sealed class AutoFixtureUnsetFieldFiller(IFixture fixture) : IUnsetFieldFiller
{
    private readonly HashSet<PropertyInfo> excludedFields = [];

    /// <summary>Opt field out of this filler entirely - it stays exactly as XFTY left it. Chainable.</summary>
    public AutoFixtureUnsetFieldFiller Excluding(PropertyInfo field)
    {
        _ = this.excludedFields.Add(field);
        return this;
    }

    public void Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields)
    {
        SpecimenContext context = new(fixture);
        foreach (PropertyInfo field in unsetFields)
        {
            if (!this.excludedFields.Contains(field))
            {
                this.FillOne(record, field, context);
            }
        }
    }

    private void FillOne(object record, PropertyInfo field, SpecimenContext context)
    {
        try
        {
            object? generated = context.Resolve(field.PropertyType);
            if (generated is not OmitSpecimen)
            {
                field.SetValue(record, generated);
            }
        }
        catch (ObjectCreationException)
        {
            // A field that circles back on its own type, under the fixture's default
            // ThrowingRecursionBehavior - leave it exactly as XFTY left it.
        }
    }
}
