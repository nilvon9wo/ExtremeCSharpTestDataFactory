using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/child-records.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExChildRecordsTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    private static IProviderLookup LookupWithCase() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
            [LookupKey.Get(typeof(Case))] = new BlankCaseProvider(),
        });

    [Fact]
    public void TheHeadlineExample()
    {
        // from docs/use/child-records.md, top of the page
        Bundle bundle = new RecordProvider(typeof(Account), Lookup)
            .SetInsertMode(InsertMode.Mock)
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "Buyer" }).SetQuantity(3))
            .SupplyBundle();

        object account = bundle.PrimaryRecords()![0];
        List<object> contacts = bundle.GetChildList<Contact>(x => x.AccountId);

        Assert.Equal(3, contacts.Count);
        Assert.All(contacts.Cast<Contact>(), contact => Assert.Equal(((Account)account).Id, contact.AccountId));
    }

    [Fact]
    public void ChildProviderConstructors()
    {
        // from docs/use/child-records.md "ChildProvider"
        ChildProvider blank = new(Field.Of<Contact>(x => x.AccountId));
        ChildProvider withTemplate = new(Field.Of<Contact>(x => x.AccountId), new Contact { Department = "Buyer" });

        Assert.Equal(typeof(Contact), blank.ChildType);
        Assert.Equal(typeof(Contact), withTemplate.ChildType);
        _ = blank.SetQuantity(3);
    }

    [Fact]
    public void AttachingIt_Additive()
    {
        // from docs/use/child-records.md "Attaching it"
        Bundle bundle = new RecordProvider(typeof(Account), LookupWithCase())
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "A" }).SetQuantity(3))
            .With(ChildProvider.For<Contact>(x => x.AccountId, new Contact { Department = "B" }).SetQuantity(2))  // additive
            .With(ChildProvider.For<Case>(x => x.AccountId).SetQuantity(2))                                      // another type
            .SetInsertMode(InsertMode.Mock)
            .SupplyBundle();

        Assert.Equal(5, bundle.GetChildList<Contact>(x => x.AccountId).Count);
        Assert.Equal(2, bundle.GetChildList<Case>(x => x.AccountId).Count);
    }

    [Fact]
    public void Grandchildren_ChildProviderNests()
    {
        // from docs/use/child-records.md "Grandchildren"
        Bundle bundle = new RecordProvider(typeof(Account), LookupWithCase())
            .SetInsertMode(InsertMode.Mock)
            .With(
                ChildProvider.For<Contact>(x => x.AccountId).SetQuantity(3)
                    .With(ChildProvider.For<Case>(x => x.ContactId).SetQuantity(2)))
            .SupplyBundle();

        List<object> cases = bundle.GetChildBundle<Contact>(x => x.AccountId)!
            .GetChildList<Case>(x => x.ContactId);

        Assert.Equal(3, bundle.GetChildList<Contact>(x => x.AccountId).Count);
        Assert.Equal(6, cases.Count);
    }
}

file sealed class BlankCaseProvider : IRecordProvider
{
    private MasterTemplate Template { get; } = new MasterTemplate<Case>(x => x.Id);

    public System.Reflection.PropertyInfo PrimaryTargetField => Field.Of<Case>(x => x.Id);

    public MasterTemplate MasterTemplate => this.Template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}
