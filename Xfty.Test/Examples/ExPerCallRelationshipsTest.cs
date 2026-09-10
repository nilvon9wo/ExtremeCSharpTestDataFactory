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
/// Runs the exact code shown in docs/use/per-call-relationships.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExPerCallRelationshipsTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Contact>()] = new ContactRequiringAccountProvider(),
            [LookupKey.Get<Account>()] = new AccountWithOptionalOwnerAndParentProvider(),
            [LookupKey.Get<User>()] = new LeafUserProvider(),
        });

    [Fact]
    public async Task TheSimplestCase()
    {
        // from docs/use/per-call-relationships.md "The simplest case"
        Account result = await new RecordProvider<Account>(Lookup())
            .IncludeOptional(x => x.OwnerId)       // generate this optional one too
            .ExcludeRelationship(x => x.ParentId)  // do not generate this one, even though it is required
            .SetInsertMode(InsertMode.Mock)
            .Supply().ConfigureAwait(true);

        Assert.NotNull(result.OwnerId);
        Assert.Null(result.ParentId);
    }

    [Fact]
    public async Task ReachingDeeper_APath()
    {
        // from docs/use/per-call-relationships.md "Reaching deeper - a path"
        Bundle bundle = await new RecordProvider<Contact>(Lookup())
            .IncludeOptional([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)])
            .SetInclusivity(InsertInclusivity.Required)
            .SupplyBundle().ConfigureAwait(true);

        Bundle accountBundle = bundle.GetBundle<Contact>(x => x.AccountId)!;
        Assert.NotNull(accountBundle.GetList<Account>(x => x.OwnerId));
    }
}

file sealed class ContactRequiringAccountProvider : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<Contact>(x => x.Id)
        .PutRequired(x => x.AccountId, new DefaultRelationship(new Account()));

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}

file sealed class AccountWithOptionalOwnerAndParentProvider : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<Account>(x => x.Id)
        .PutOptional(x => x.OwnerId, new DefaultRelationship(new User()))
        .PutOptional(x => x.ParentId, new DefaultRelationship(new Account()));

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}

file sealed class LeafUserProvider : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<User>(x => x.Id);

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}