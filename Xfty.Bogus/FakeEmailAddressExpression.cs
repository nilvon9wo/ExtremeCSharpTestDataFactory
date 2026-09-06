using global::Bogus;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Bogus;

/// <summary>
/// An <see cref="IValueExpression"/> producing a realistic-looking email
/// address via Bogus - unlike <see cref="Values.UniqueEmailExpression"/>,
/// not guaranteed unique within a process, since Bogus generates from a
/// finite name/domain pool rather than a counter.
/// </summary>
public sealed class FakeEmailAddressExpression(string locale = "en") : IValueExpression
{
    private readonly Faker faker = new(locale);

    public object Get() => this.faker.Internet.Email();
}
