using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/generating-records.md and
/// docs/use/getting-started.md, proving those examples compile and behave as
/// documented. Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExGeneratingRecordsTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public async Task Supply_TheSimplestCase_ReturnsOneRecord()
    {
        // from docs/use/generating-records.md "One record"
        Contact result = await new RecordProvider<Contact>(Lookup)
            .Supply().ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Null(result.Id); // not inserted by default
    }

    [Fact]
    public async Task ShorthandConstructors_FromDocs_AllWork()
    {
        // from docs/use/generating-records.md "Shorthand constructors"
        Contact fromTemplate = (Contact)await new RecordProvider(new Contact { FirstName = "Alice" }, Lookup).Supply().ConfigureAwait(true);
        List<object> fromList = await new RecordProvider([new Contact(), new Contact()], Lookup).SupplyList().ConfigureAwait(true);
        object fromKey = await new RecordProvider(LookupKey.Get<Contact>(), Lookup).Supply().ConfigureAwait(true);

        Assert.Equal("Alice", fromTemplate.FirstName);
        Assert.Equal(2, fromList.Count);
        Assert.NotNull(fromKey);
    }

    [Fact]
    public async Task GettingStarted_CreatingYourFirstRecord()
    {
        // from docs/use/getting-started.md "Creating Your First Record"
        DefaultProviderLookup lookup = new();

        Contact contact = await new RecordProvider<Contact>(lookup)
            .Supply().ConfigureAwait(true);

        Assert.NotNull(contact);
    }

    [Fact]
    public async Task GettingStarted_OverrideTemplates()
    {
        // from docs/use/getting-started.md "Override Templates"
        DefaultProviderLookup lookup = new();

        Contact contact = await new RecordProvider<Contact>(lookup)
            .SetOverrideTemplate(new Contact { FirstName = "Alice", LastName = "Smith" })
            .Supply().ConfigureAwait(true);

        Assert.Equal("Alice", contact.FirstName);
        Assert.Equal("Smith", contact.LastName);
    }

    [Fact]
    public async Task GettingStarted_ShorthandConstructors()
    {
        // from docs/use/getting-started.md "Shorthand Constructors"
        DefaultProviderLookup lookup = new();

        Contact fromTemplate = (Contact)await new RecordProvider(new Contact { FirstName = "Alice" }, lookup).Supply().ConfigureAwait(true);
        List<object> fromList = await new RecordProvider([new Contact(), new Contact()], lookup).SupplyList().ConfigureAwait(true);
        object fromKey = await new RecordProvider(LookupKey.Get<Contact>(), lookup).Supply().ConfigureAwait(true);

        Assert.Equal("Alice", fromTemplate.FirstName);
        Assert.Equal(2, fromList.Count);
        Assert.NotNull(fromKey);
    }

    [Fact]
    [SuppressMessage("Performance", "HLQ005:Avoid Single() and SingleOrDefault()", Justification = "<Pending>")]
    public async Task GettingStarted_UnderstandingBundles()
    {
        // from docs/use/getting-started.md "Understanding Bundles" - a Case pulling in an Account
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Case>()] = new CaseWithAccountProvider(),
            [LookupKey.Get<Account>()] = new AccountDataProvider(),
        });
        Bundle bundle = await new RecordProvider<Case>(lookup)
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle().ConfigureAwait(true);

        List<object> accounts = bundle.GetList<Case>(x => x.AccountId)!;
        Bundle? accountBundle = bundle.GetBundle<Case>(x => x.AccountId);

        _ = Assert.Single(accounts);
        Assert.NotNull(accountBundle);
    }
}

file sealed class CaseWithAccountProvider : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<Case>(x => x.Id)
        .PutRequired(x => x.AccountId, new DefaultRelationship(new Account()));

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}