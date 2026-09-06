using Net.NowhereAtAll.Xfty.Core;
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
        List<object> results = await new RecordProvider(typeof(Contact), Lookup)
            .Put<Contact>(x => x.FirstName, new IncrementingStringExpression("Test Contact"))
            .SetQuantityPerTemplate(3)
            .SupplyList();

        Assert.Equal(["Test Contact 1", "Test Contact 2", "Test Contact 3"], results.Cast<Contact>().Select(c => c.FirstName));
    }

    [Fact]
    public async Task ImplicitExactValues()
    {
        // from docs/use/value-expressions.md "Implicit exact values"
        Account withImplicitLiterals = (Account)await new RecordProvider(typeof(Account), Lookup)
            .Put<Account>(x => x.Type, "Customer")
            .Put<Account>(x => x.NumberOfEmployees, 500)
            .Supply();

        Account withExplicitLiterals = (Account)await new RecordProvider(typeof(Account), Lookup)
            .Put<Account>(x => x.Type, new LiteralExpression("Customer"))
            .Put<Account>(x => x.NumberOfEmployees, new LiteralExpression(500))
            .Supply();

        Assert.Equal(withExplicitLiterals.Type, withImplicitLiterals.Type);
        Assert.Equal(withExplicitLiterals.NumberOfEmployees, withImplicitLiterals.NumberOfEmployees);
    }
}
