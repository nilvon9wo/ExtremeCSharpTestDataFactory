using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/enrichment.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExEnrichmentTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    private static Bundle SampleBundle() =>
        new RecordProvider(typeof(Contact), Lookup)
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetQuantityPerTemplate(2)
            .SupplyBundle();

    [Fact]
    public void InjectAll_TheHeadlineExample()
    {
        // from docs/use/enrichment.md "InjectAll - everything the graph holds"
        Bundle bundle = SampleBundle();

        List<object> contacts = bundle.InjectAll(Field.Of<Contact>(x => x.Id));

        Assert.NotNull(((Contact)contacts[0]).Account!.Name);      // the generated ancestor - was null
        Assert.NotNull(((Contact)contacts[0]).Account!.Contacts);  // the inverse child
    }

    [Fact]
    public void InjectWithABroadStart()
    {
        // from docs/use/enrichment.md - configuring a broad pass
        Bundle bundle = SampleBundle();

        List<object> result = bundle.Inject(Field.Of<Contact>(x => x.Id), InjectConfig.AllParents().ParentDepth(2));

        Assert.NotEmpty(result);
    }

    [Fact]
    public void InjectOneChildCollectionNothingElse()
    {
        // from docs/use/enrichment.md "Inject(field, config) - name exactly what you want"
        Bundle bundle = new RecordProvider(typeof(Account), Lookup)
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(x => x.AccountId), 2)
            .SupplyBundle();

        List<object> result = bundle.Inject(Field.Of<Account>(x => x.Id), InjectConfig.Nothing().InjectChild(Field.Of<Contact>(x => x.AccountId)));

        Assert.Equal(2, ((Account)result[0]).Contacts!.Count);
    }

    [Fact]
    public void InjectAScalarAndAValueTwoHopsUp()
    {
        // from docs/use/enrichment.md "a scalar the platform would compute, and a value two hops up"
        Bundle bundle = SampleBundle();

        InjectConfig config = InjectConfig.Nothing()
            .InjectValue(Field.Of<Contact>(x => x.Birthdate), new DateTime(2020, 1, 1))
            .InjectValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.AnnualRevenue)], 7_500_000m);

        List<object> result = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        Assert.Equal(new DateTime(2020, 1, 1), ((Contact)result[0]).Birthdate);
        Assert.Equal(7_500_000m, ((Contact)result[0]).Account!.AnnualRevenue);
    }

    [Fact]
    public void ForcingAValueThatDependsOnTheGraph()
    {
        // from docs/use/enrichment.md "Runs after generation"
        Bundle bundle = SampleBundle();

        object? parentName = bundle.GetValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)]);
        InjectConfig config = InjectConfig.Nothing()
            .InjectValue(Field.Of<Contact>(x => x.Department), $"{parentName} (contact)");
        List<object> result = bundle.Inject(Field.Of<Contact>(x => x.Id), config);

        Assert.Equal($"{parentName} (contact)", ((Contact)result[0]).Department);
    }
}
