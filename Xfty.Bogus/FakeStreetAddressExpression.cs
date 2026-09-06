using global::Bogus;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Bogus;

/// <summary>
/// An <see cref="IValueExpression"/> producing a realistic-looking street
/// address via Bogus.
/// </summary>
public sealed class FakeStreetAddressExpression(string locale = "en") : IValueExpression
{
    private readonly Faker faker = new(locale);

    public object Get() => this.faker.Address.StreetAddress();
}
