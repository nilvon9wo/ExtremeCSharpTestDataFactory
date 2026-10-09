using Bogus;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Bogus;

/// <summary>
/// An <see cref="IValueYielding"/> producing a realistic-looking street
/// address via Bogus.
/// </summary>
public sealed class FakeStreetAddressExpression(string locale = "en") : IValueYielding
{
    private readonly Faker _faker = new(locale);

    public object Get() => this._faker.Address.StreetAddress();
}