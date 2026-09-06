using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/override-templates.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExOverrideTemplatesTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public async Task TheSimplestCase()
    {
        // from docs/use/override-templates.md "The simplest case"
        Contact result = (Contact)await new RecordProvider(typeof(Contact), Lookup)
            .SetOverrideTemplate(new Contact { FirstName = "Alice", LastName = "Smith" })
            .Supply();

        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.NotNull(result.Email); // still generated

        // the shorthand constructor form
        Contact shorthand = (Contact)await new RecordProvider(new Contact { FirstName = "Alice" }, Lookup)
            .Supply();
        Assert.Equal("Alice", shorthand.FirstName);
    }

    [Fact]
    public async Task Precedence_TheOverrideTemplateWins()
    {
        // from docs/use/override-templates.md "Precedence"
        Contact result = (Contact)await new RecordProvider(typeof(Contact), Lookup)
            .Put<Contact>(x => x.FirstName, new LiteralExpression("Generated"))
            .SetOverrideTemplate(new Contact { FirstName = "Alice" })
            .Supply();

        Assert.Equal("Alice", result.FirstName); // not "Generated"
    }

    [Fact]
    public async Task RemovingValues()
    {
        // from docs/use/override-templates.md "Removing values"
        Contact result = (Contact)await new RecordProvider(typeof(Contact), Lookup)
            .RemoveFromMasterTemplate<Contact>(x => x.Email)
            .Supply();

        Assert.Null(result.Email);
    }
}
