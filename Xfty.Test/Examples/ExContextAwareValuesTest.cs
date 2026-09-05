using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/context-aware-values.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExContextAwareValuesTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public void CopyASiblingField()
    {
        // from docs/use/context-aware-values.md "Copy a sibling field"
        Account result = (Account)new RecordProvider(typeof(Account), Lookup)
            .Put<Account>(x => x.ShippingCity, "Berlin")
            .Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCity))
            .Supply();

        Assert.Equal("Berlin", result.BillingCity);
    }

    [Fact]
    public void CopyAFieldFromAGeneratedAncestor_OneHop()
    {
        // from docs/use/context-aware-values.md "Copy a field from a generated ancestor" (one hop)
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup)
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account { Site = "HQ" }))
            .Put<Contact>(x => x.Department, CopyFromAncestorExpression.From<Contact, Account>(x => x.AccountId, x => x.Site))
            .SetInclusivity(InsertInclusivity.Required)
            .Supply();

        Assert.Equal("HQ", result.Department);
    }

    [Fact]
    public void CopyAFieldFromAGeneratedAncestor_MultiHop()
    {
        // from docs/use/context-aware-values.md "Copy a field from a generated ancestor" (several hops)
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Case))] = new CaseUnderAccountProvider(),
            [LookupKey.Get(typeof(Account))] = new AccountWithOwnerProvider(),
            [LookupKey.Get(typeof(User))] = new LeafUserProvider(),
        });
        Case result = (Case)new RecordProvider(typeof(Case), lookup)
            .Put<Case>(x => x.Subject, new CopyFromAncestorExpression([
                Field.Of<Case>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId), Field.Of<User>(x => x.LastName),
            ]))
            .SetInclusivity(InsertInclusivity.Required)
            .Supply();

        Assert.NotNull(result.Subject);
    }

    [Fact]
    public void YourOwnLogic_CustomContextAwareExpression()
    {
        // from docs/use/context-aware-values.md "Your own logic"
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup)
            .Put<Contact>(x => x.Birthdate, new DateTime(2010, 1, 1))
            .Put<Contact>(x => x.Department, new IsMinorFlag())
            .Supply();

        Assert.Equal("MINOR", result.Department);
    }

    [Fact]
    public void HowItRuns_TheOneOrderingRule()
    {
        // from docs/use/context-aware-values.md "How it runs, and the one ordering rule" - the wrong-order example throws
        // (a bare MasterTemplate, not a Provider with its own pre-existing field-order, keeps the example's
        // "BillingCity put before ShippingCity" actually the wrong order at generation time)
        RecordProvider provider = new RecordProvider(typeof(Account), new BlankAccountProviderLookup())
            .Put<Account>(x => x.BillingCity, CopyFromSiblingExpression.From<Account>(x => x.ShippingCity))
            .Put<Account>(x => x.ShippingCity, CopyFromSiblingExpression.From<Account>(x => x.Site));

        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => provider.Supply());
        Assert.Contains("ShippingCity", thrown.Message);
    }

    [Fact]
    public void ReadingUpFromAChild()
    {
        // from docs/use/context-aware-values.md "Reading up from a child"
        Account parent = new();
        Contact child = new() { Department = "Field Ops" };
        DeferredGraph graph = new(
            [parent, child],
            [new DepthBatchedInserterParentLink(childIndex: 1, parentIndex: 0, Field.Of<Contact>(x => x.AccountId))]);
        CopyFromDescendantExpression expression = new(
            Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department));

        object? actualResult = expression.Get(graph, 0);

        Assert.Equal("Field Ops", actualResult);
    }

    [Fact]
    public void ReadingUpFromAChild_MultiHop()
    {
        // from docs/use/context-aware-values.md "Reading up from a child" (several hops)
        Account grandparent = new();
        Contact parent = new();
        Case grandchild = new() { Subject = "Escalated" };
        DeferredGraph graph = new(
            [grandparent, parent, grandchild],
            [
                new DepthBatchedInserterParentLink(childIndex: 1, parentIndex: 0, Field.Of<Contact>(x => x.AccountId)),
                new DepthBatchedInserterParentLink(childIndex: 2, parentIndex: 1, Field.Of<Case>(x => x.ContactId)),
            ]);
        CopyFromDescendantExpression expression = new([
            Field.Of<Contact>(x => x.AccountId), Field.Of<Case>(x => x.ContactId), Field.Of<Case>(x => x.Subject),
        ]);

        object? actualResult = expression.Get(graph, 0);

        Assert.Equal("Escalated", actualResult);
    }

    [Fact]
    public void ReadingUpFromAChild_NeedsDeferredMode()
    {
        // from docs/use/context-aware-values.md - "it only works under Deferred (or .DepthBatched())"
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountReadingChildDepartmentProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactUnderAccountProvider(),
        });
        RecordProvider provider = new RecordProvider(typeof(Contact), lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Deferred)
            .Put<Contact>(x => x.Department, "Field Ops");

        Bundle bundle = provider.SupplyBundle();
        DeferredInsertBuffer graph = DeferredInsertBuffer.Flatten(bundle);
        Account account = (Account)graph.Records().OfType<Account>().Single();

        Assert.Equal("Field Ops", account.Site);
    }
}

file sealed class IsMinorFlag : IContextAwareExpression
{
    public object? Get(GenerationContext context)
    {
        DateTime? birthdate = (DateTime?)context.SiblingValue(Field.Of<Contact>(x => x.Birthdate));
        return birthdate is not null && birthdate.Value.AddYears(18) > DateTime.Today ? "MINOR" : "ADULT";
    }
}

file sealed class CaseUnderAccountProvider()
    : SimpleRecordProvider<Case>(
        new MasterTemplate<Case>(x => x.Id)
            .PutRequired(x => x.AccountId, new DefaultRelationship(new Account())));

file sealed class AccountWithOwnerProvider()
    : SimpleRecordProvider<Account>(
        new MasterTemplate<Account>(x => x.Id)
        {
            [x => x.Name] = new IncrementingStringExpression("Acct"),
        }.PutRequired(x => x.OwnerId, new DefaultRelationship(new User())));

file sealed class LeafUserProvider()
    : SimpleRecordProvider<User>(
        new MasterTemplate<User>(x => x.Id)
        {
            [x => x.LastName] = new IncrementingStringExpression("User"),
        });

/// <summary>A lookup whose Account provider carries no pre-existing field defaults, so a test controls the value-field order entirely itself.</summary>
file sealed class BlankAccountProviderLookup : IProviderLookup
{
    public IRecordProvider Get(Type recordType) => new BlankAccountProvider();

    public IRecordProvider Get(ILookupKey lookupKey) => new BlankAccountProvider();

    public ISet<ILookupKey> KeysFor(object? record) => new HashSet<ILookupKey> { LookupKey.Get(typeof(Account)) };
}

file sealed class BlankAccountProvider()
    : SimpleRecordProvider<Account>(new MasterTemplate<Account>(x => x.Id));

/// <summary>An Account whose Site is copied up from the Contact that references it.</summary>
file sealed class AccountReadingChildDepartmentProvider : IRecordProvider
{
    // on the Account Provider - from docs/use/context-aware-values.md "Reading up from a child"
    // and docs/use/advanced/matching-values.md "Child value up onto a parent" - the doc's own
    // .Put<Account>(...) chain form is kept here verbatim, so this stays off SimpleRecordProvider.
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Account>(x => x.Id))
        .Put<Account>(x => x.Name, new IncrementingStringExpression("Acct"))
        .Put<Account>(x => x.Site, CopyFromDescendantExpression.From<Contact>(x => x.AccountId, x => x.Department));

    public System.Reflection.PropertyInfo PrimaryTargetField => Field.Of<Account>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}

file sealed class ContactUnderAccountProvider()
    : SimpleRecordProvider<Contact>(
        new MasterTemplate<Contact>(x => x.Id)
            .PutRequired(x => x.AccountId, new DefaultRelationship(new Account())));
