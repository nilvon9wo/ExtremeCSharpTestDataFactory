using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;

namespace Net.Nowhereatall.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/generating-records.md and
/// docs/use/getting-started.md, proving those examples compile and behave as
/// documented. Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExGeneratingRecordsTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public void Supply_TheSimplestCase_ReturnsOneRecord()
    {
        // from docs/use/generating-records.md "One record"
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup)
            .Supply();

        Assert.NotNull(result);
        Assert.Null(result.Id); // not inserted by default
    }

    [Fact]
    public void ShorthandConstructors_FromDocs_AllWork()
    {
        // from docs/use/generating-records.md "Shorthand constructors"
        Contact fromTemplate = (Contact)new RecordProvider(new Contact { FirstName = "Alice" }, Lookup).Supply();
        List<object> fromList = new RecordProvider(new List<object> { new Contact(), new Contact() }, Lookup).SupplyList();
        object fromKey = new RecordProvider(LookupKey.Get(typeof(Contact)), Lookup).Supply();

        Assert.Equal("Alice", fromTemplate.FirstName);
        Assert.Equal(2, fromList.Count);
        Assert.NotNull(fromKey);
    }

    [Fact]
    public void GettingStarted_CreatingYourFirstRecord()
    {
        // from docs/use/getting-started.md "Creating Your First Record"
        DefaultProviderLookup providerLookup = new();

        Contact contact = (Contact)new RecordProvider(typeof(Contact), providerLookup)
            .Supply();

        Assert.NotNull(contact);
    }

    [Fact]
    public void GettingStarted_OverrideTemplates()
    {
        // from docs/use/getting-started.md "Override Templates"
        DefaultProviderLookup providerLookup = new();

        Contact contact = (Contact)new RecordProvider(typeof(Contact), providerLookup)
            .SetOverrideTemplate(new Contact { FirstName = "Alice", LastName = "Smith" })
            .Supply();

        Assert.Equal("Alice", contact.FirstName);
        Assert.Equal("Smith", contact.LastName);
    }

    [Fact]
    public void GettingStarted_ShorthandConstructors()
    {
        // from docs/use/getting-started.md "Shorthand Constructors"
        DefaultProviderLookup providerLookup = new();

        Contact fromTemplate = (Contact)new RecordProvider(new Contact { FirstName = "Alice" }, providerLookup).Supply();
        List<object> fromList = new RecordProvider(new List<object> { new Contact(), new Contact() }, providerLookup).SupplyList();
        object fromKey = new RecordProvider(LookupKey.Get(typeof(Contact)), providerLookup).Supply();

        Assert.Equal("Alice", fromTemplate.FirstName);
        Assert.Equal(2, fromList.Count);
        Assert.NotNull(fromKey);
    }

    [Fact]
    public void GettingStarted_UnderstandingBundles()
    {
        // from docs/use/getting-started.md "Understanding Bundles" - a Case pulling in an Account
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Case))] = new CaseWithAccountProvider(),
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
        });
        Bundle bundle = new RecordProvider(typeof(Case), lookup)
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();

        List<object> accounts = bundle.GetList<Case>(x => x.AccountId)!;
        Bundle? accountBundle = bundle.GetBundle<Case>(x => x.AccountId);

        _ = Assert.Single(accounts);
        Assert.NotNull(accountBundle);
    }
}

file sealed class CaseWithAccountProvider()
    : SimpleRecordProvider<Case>(
        new MasterTemplate<Case>(x => x.Id)
            .PutRequired(x => x.AccountId, new DefaultRelationship(new Account())));
