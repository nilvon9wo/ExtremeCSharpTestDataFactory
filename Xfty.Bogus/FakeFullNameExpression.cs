using global::Bogus;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Bogus;

/// <summary>
/// An <see cref="IValueExpression"/> producing a realistic-looking full name
/// via Bogus, for a Provider that wants a field to *look* real rather than
/// merely be present. See docs/reference/comparison.md for why this lives in
/// a separate package instead of core <c>Xfty</c>.
/// </summary>
public sealed class FakeFullNameExpression(string locale = "en") : IValueExpression
{
    private readonly Faker faker = new(locale);

    public object Get() => this.faker.Name.FullName();
}
