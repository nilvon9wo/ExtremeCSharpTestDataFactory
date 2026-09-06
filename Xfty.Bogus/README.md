# Xfty.Bogus

Four [`Xfty`](https://www.nuget.org/packages/Xfty) `IValueExpression`s backed
by [Bogus](https://github.com/bchavez/Bogus), for a field that needs to
*look* like real data rather than merely be present. Core `Xfty` has no
built-in realistic-data generation and never will - this is the opt-in for
it.

```bash
dotnet add package Xfty.Bogus
```

## Usage

Drop one into any Provider's Master Template exactly like a bundled `Xfty`
expression:

```csharp
using Net.Nowhereatall.Xfty.Bogus;

new MasterTemplate<Contact>(x => x.Id)
{
    [x => x.FirstName] = new FakeFullNameExpression(),
    [x => x.Email] = new FakeEmailAddressExpression(),
};
```

| Expression | Produces |
|---|---|
| `FakeFullNameExpression(locale = "en")` | A realistic full name |
| `FakeEmailAddressExpression(locale = "en")` | A realistic email address - **not** guaranteed unique within a process, unlike `Xfty`'s own `UniqueEmailExpression`, since Bogus draws from a finite name/domain pool rather than a counter |
| `FakeStreetAddressExpression(locale = "en")` | A realistic street address |
| `FakeParagraphExpression(sentenceCount = 3, locale = "en")` | A lorem-ipsum-style paragraph, for a body-text field |

Each wraps its own `Bogus.Faker` instance, so different fields/locales don't
interfere with each other.

## Full documentation

- [How XFTY compares](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/reference/comparison.md) - why realistic fake data lives in this separate package instead of core `Xfty`
- [Value expressions](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/value-expressions.md) - how `IValueExpression` and a Master Template fit together
- [Everything else `Xfty` does](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory#readme)
