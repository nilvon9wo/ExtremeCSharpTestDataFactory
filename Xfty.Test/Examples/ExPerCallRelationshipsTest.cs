using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;

namespace Net.Nowhereatall.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/per-call-relationships.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExPerCallRelationshipsTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Contact))] = new ContactRequiringAccountProvider(),
            [LookupKey.Get(typeof(Account))] = new AccountWithOptionalOwnerAndParentProvider(),
            [LookupKey.Get(typeof(User))] = new LeafUserProvider(),
        });

    [Fact]
    public async Task TheSimplestCase()
    {
        // from docs/use/per-call-relationships.md "The simplest case"
        Account result = (Account)await new RecordProvider(typeof(Account), Lookup())
            .IncludeOptional<Account>(x => x.OwnerId)       // generate this optional one too
            .ExcludeRelationship<Account>(x => x.ParentId)  // do not generate this one, even though it is required
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        Assert.NotNull(result.OwnerId);
        Assert.Null(result.ParentId);
    }

    [Fact]
    public async Task ReachingDeeper_APath()
    {
        // from docs/use/per-call-relationships.md "Reaching deeper - a path"
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup())
            .IncludeOptional([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)])
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle();

        Bundle accountBundle = bundle.GetBundle<Contact>(x => x.AccountId)!;
        Assert.NotNull(accountBundle.GetList<Account>(x => x.OwnerId));
    }
}

file sealed class ContactRequiringAccountProvider()
    : SimpleRecordProvider<Contact>(
        new MasterTemplate<Contact>(x => x.Id)
            .PutRequired(x => x.AccountId, new DefaultRelationship(new Account())));

file sealed class AccountWithOptionalOwnerAndParentProvider()
    : SimpleRecordProvider<Account>(
        new MasterTemplate<Account>(x => x.Id)
            .PutOptional(x => x.OwnerId, new DefaultRelationship(new User()))
            .PutOptional(x => x.ParentId, new DefaultRelationship(new Account())));

file sealed class LeafUserProvider()
    : SimpleRecordProvider<User>(new MasterTemplate<User>(x => x.Id));
