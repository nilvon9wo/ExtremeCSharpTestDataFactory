using global::Bogus;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Bogus;

/// <summary>
/// An <see cref="IValueExpression"/> producing a realistic-looking street
/// address via Bogus.
/// </summary>
public sealed class FakeStreetAddressExpression : IValueExpression
{
    private readonly Faker faker;

    public FakeStreetAddressExpression(string locale = "en") => this.faker = new Faker(locale);

    public object Get() => this.faker.Address.StreetAddress();
}
