using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/value-expressions.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExValueExpressionsTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public async Task PutAnExpression()
    {
        // from docs/use/value-expressions.md "Put(...) an expression"
        List<Contact> results = await new RecordProvider<Contact>(Lookup)
            .Put(x => x.FirstName, new IncrementingStringExpression("Test Contact"))
            .SetQuantityPerTemplate(3)
            .SupplyList().ConfigureAwait(true);

        Assert.Equal(["Test Contact 1", "Test Contact 2", "Test Contact 3"], results.Cast<Contact>().Select(c => c.FirstName));
    }

    [Fact]
    public async Task ImplicitExactValues()
    {
        // from docs/use/value-expressions.md "Implicit exact values"
        Account withImplicitLiterals = await new RecordProvider<Account>(Lookup)
            .Put(x => x.Type, "Customer")
            .Put(x => x.NumberOfEmployees, 500)
            .Supply().ConfigureAwait(true);

        Account withExplicitLiterals = await new RecordProvider<Account>(Lookup)
            .Put(x => x.Type, new LiteralExpression("Customer"))
            .Put(x => x.NumberOfEmployees, new LiteralExpression(500))
            .Supply().ConfigureAwait(true);

        Assert.Equal(withExplicitLiterals.Type, withImplicitLiterals.Type);
        Assert.Equal(withExplicitLiterals.NumberOfEmployees, withImplicitLiterals.NumberOfEmployees);
    }
}