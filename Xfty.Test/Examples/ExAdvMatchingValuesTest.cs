using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/advanced/matching-values.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExAdvMatchingValuesTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public void SameRecord_AContextAwareSibling()
    {
        // from docs/use/advanced/matching-values.md "Same record - a context-aware sibling"
        Account result = (Account)new RecordProvider(typeof(Account), Lookup)
            .Put<Account>(x => x.ShippingCountry, "Germany")
            .Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCountry))
            .Supply();

        Assert.Equal("Germany", result.BillingCity);
    }
}
