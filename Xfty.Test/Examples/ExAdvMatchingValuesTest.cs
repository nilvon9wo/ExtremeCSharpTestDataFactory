using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/advanced/matching-values.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExAdvMatchingValuesTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public async Task SameRecord_AContextAwareSibling()
    {
        // from docs/use/advanced/matching-values.md "Same record - a context-aware sibling"
        Account result = (Account)await new RecordProvider(typeof(Account), Lookup)
            .Put<Account>(x => x.ShippingCountry, "Germany")
            .Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCountry))
            .Supply();

        Assert.Equal("Germany", result.BillingCity);
    }
}
