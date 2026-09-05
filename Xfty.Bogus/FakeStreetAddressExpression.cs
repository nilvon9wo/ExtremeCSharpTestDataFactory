using global::Bogus;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Bogus;

/// <summary>
/// An <see cref="IValueExpression"/> producing a realistic-looking street
/// address via Bogus.
/// </summary>
public sealed class FakeStreetAddressExpression(string locale = "en") : IValueExpression
{
    private readonly Faker faker = new Faker(locale);

    public object Get() => this.faker.Address.StreetAddress();
}
