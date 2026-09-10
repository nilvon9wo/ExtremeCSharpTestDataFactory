using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Examples;

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
        Contact result = await new RecordProvider<Contact>(Lookup)
            .SetOverrideTemplate(new Contact { FirstName = "Alice", LastName = "Smith" })
            .Supply().ConfigureAwait(true);

        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.NotNull(result.Email); // still generated

        // the shorthand constructor form
        Contact shorthand = (Contact)await new RecordProvider(new Contact { FirstName = "Alice" }, Lookup)
            .Supply().ConfigureAwait(true);
        Assert.Equal("Alice", shorthand.FirstName);
    }

    [Fact]
    public async Task Precedence_TheOverrideTemplateWins()
    {
        // from docs/use/override-templates.md "Precedence"
        Contact result = await new RecordProvider<Contact>(Lookup)
            .Put(x => x.FirstName, new LiteralExpression("Generated"))
            .SetOverrideTemplate(new Contact { FirstName = "Alice" })
            .Supply().ConfigureAwait(true);

        Assert.Equal("Alice", result.FirstName); // not "Generated"
    }

    [Fact]
    public async Task RemovingValues()
    {
        // from docs/use/override-templates.md "Removing values"
        Contact result = await new RecordProvider<Contact>(Lookup)
            .RemoveFromMasterTemplate(x => x.Email)
            .Supply().ConfigureAwait(true);

        Assert.Null(result.Email);
    }
}
