using global::Bogus;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Bogus;

/// <summary>
/// An <see cref="IValueExpression"/> producing a lorem-ipsum-style paragraph
/// via Bogus, for a body-text field that needs to look populated rather than
/// hold a literal placeholder.
/// </summary>
public sealed class FakeParagraphExpression : IValueExpression
{
    private const int DefaultSentenceCount = 3;

    private readonly Faker faker;
    private readonly int sentenceCount;

    public FakeParagraphExpression(int sentenceCount = DefaultSentenceCount, string locale = "en")
    {
        this.faker = new Faker(locale);
        this.sentenceCount = sentenceCount;
    }

    public object Get() => this.faker.Lorem.Paragraph(this.sentenceCount);
}
