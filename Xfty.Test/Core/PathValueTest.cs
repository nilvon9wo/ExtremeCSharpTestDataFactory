using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>
/// Proves Put(List&lt;PropertyInfo&gt; path, value) - path-scoped value overrides
/// that land on a generated ancestor, forcing every relationship on the way
/// (like IncludeOptional). Mock mode throughout.
///
/// Apex's deep-two-hop case chains Case -&gt; Contact -&gt; Account; this demo
/// domain has no Case, so the same shape (two forced relationship hops) is
/// proven with Contact -&gt; Account -&gt; Account (self-referencing ParentId)
/// instead - AccountWithOptionalParentProvider exists only for that one test.
/// </summary>
public class PathValueTest
{
    private const string SharedAcctName = "path-value-test-shared-acct";

    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactWithOptionalManagerProvider(),
            [LookupKey.Get(typeof(User))] = new LeafUserProvider(),
        });

    private static IProviderLookup DeepAccountLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountWithOptionalParentProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactWithOptionalManagerProvider(),
        });

    private static IProviderLookup SharedParentLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactUnderSharedAccountProvider(),
        });

    // A value landing on the generated ancestor ------------------------

    [Fact]
    public void Put_WithALiteralPathValue_LandsItOnTheGeneratedAncestor()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Put([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Industry)], "Aerospace");

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - the path literal overrode the Account Provider default
        Account generatedAccount = (Account)bundle.GetBundle<Contact>(x => x.AccountId)!.PrimaryRecords()![0];
        Assert.Equal("Aerospace", generatedAccount.Industry);
    }

    [Fact]
    public void Put_WithAValueExpressionPathValue_RunsItOncePerGeneratedAncestor()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetQuantityPerTemplate(3)
            .SetInclusivity(InsertInclusivity.Required)
            .Put(
                [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)],
                new IncrementingStringExpression("Path Account"));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        List<object> accounts = bundle.GetBundle<Contact>(x => x.AccountId)!.PrimaryRecords()!;
        Assert.Equal(3, accounts.Count);
        HashSet<string?> names = [.. accounts.Cast<Account>().Select(account => account.Name)];
        Assert.Equal(3, names.Count); // the incrementing strategy ran once per generated Account
    }

    [Fact]
    public void Put_WithAContextAwarePathValue_EvaluatesItOnTheGeneratedAncestor()
    {
        // Arrange - the generated Account's Site copies its own Name (a sibling on the ancestor)
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Put(
                [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Site)],
                CopyFromSiblingExpression.From<Account>(x => x.Name));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Account generatedAccount = (Account)bundle.GetBundle<Contact>(x => x.AccountId)!.PrimaryRecords()![0];
        Assert.Equal(generatedAccount.Name, generatedAccount.Site); // context-aware value evaluated on the ancestor
    }

    [Fact]
    public void PutRequired_WithARelationshipPathValue_GivesTheAncestorItsOwnGeneratedParent()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .PutRequired(
                [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)],
                new DefaultRelationship(new User()));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Account generatedAccount = (Account)bundle.GetBundle<Contact>(x => x.AccountId)!.PrimaryRecords()![0];
        Assert.NotNull(generatedAccount.OwnerId); // the Account got a generated Owner via the path
    }

    [Fact]
    public void Put_WithADeepTwoRelationshipPath_WalksBothHopsAndSetsTheTargetField()
    {
        // Arrange - Contact -> Account (Contact.AccountId) -> Account (self-referencing ParentId), set the grandparent's Industry
        RecordProvider provider = new RecordProvider(typeof(Contact), DeepAccountLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .AllowAncestorCycles() // Account -> Account (self-referencing ParentId) terminates on its own after one level
            .Put(
                [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.Industry)],
                "DeepValue");

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Bundle accountBundle = bundle.GetBundle<Contact>(x => x.AccountId)!;
        Account generatedParent = (Account)accountBundle.PrimaryRecords()![0];
        Account generatedGrandparent = (Account)accountBundle.GetBundle<Account>(x => x.ParentId)!.PrimaryRecords()![0];
        Assert.NotNull(generatedParent.Id);
        Assert.Equal("DeepValue", generatedGrandparent.Industry);
    }

    [Fact]
    public void Put_AtTheDefaultNoneInclusivity_StillForcesTheNamedAncestor()
    {
        // Arrange - naming Contact.AccountId in a path value forces that ancestor even at None
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .Put([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Industry)], "Aerospace");

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Account generatedAccount = (Account)bundle.GetBundle<Contact>(x => x.AccountId)!.PrimaryRecords()![0];
        Assert.Equal("Aerospace", generatedAccount.Industry);
    }

    [Fact]
    public void PutRequired_WithARelationshipPathValue_DrivesAnEntireDeepAncestorHierarchy()
    {
        // Arrange - Contact -> Account (path) -> Account.OwnerId := User (path value) -> that User's
        // own required Manager (distinct Provider) -> that Manager's required skip-level Manager.
        // Every level generates at the default (None) inclusivity because each step is named.
        ILookupKey mgrKey = FlavouredLookupKey.Get(typeof(User), "mgr");
        ILookupKey skipKey = FlavouredLookupKey.Get(typeof(User), "skip");
        IProviderLookup deepLookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactWithOptionalManagerProvider(),
            [LookupKey.Get(typeof(User))] = new ChainedUserProvider(mgrKey),
            [mgrKey] = new ChainedUserProvider(skipKey),
            [skipKey] = new LeafUserProvider(),
        });
        RecordProvider provider = new RecordProvider(typeof(Contact), deepLookup)
            .SetInsertMode(InsertMode.Mock)
            .PutRequired(
                [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.OwnerId)],
                new DefaultRelationship(new User()));

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - every level of the chain generated
        Bundle accountBundle = bundle.GetBundle<Contact>(x => x.AccountId)!;
        Assert.NotNull(accountBundle); // Account forced by the path
        Account generatedAccount = (Account)accountBundle.PrimaryRecords()![0];
        Assert.NotNull(generatedAccount.OwnerId); // Account.OwnerId forced by the relationship path value

        Bundle ownerBundle = accountBundle.GetBundle<Account>(x => x.OwnerId)!;
        Assert.NotNull(ownerBundle); // the owner User generated
        Assert.NotNull(((User)ownerBundle.PrimaryRecords()![0]).ManagerId); // the owner generated its own Manager

        Bundle managerBundle = ownerBundle.GetBundle<User>(x => x.ManagerId)!;
        Assert.NotNull(managerBundle); // the Manager User (distinct Provider) generated
        Assert.NotNull(((User)managerBundle.PrimaryRecords()![0]).ManagerId); // the Manager generated its skip-level Manager
        Assert.NotNull(managerBundle.GetBundle<User>(x => x.ManagerId)); // and that skip-level User is in the bundle
    }

    [Fact]
    public void Put_WhenAnOptionalRelationshipIsNamedByAPathValue_ForcesItAndSetsAFieldOnIt()
    {
        // Arrange - Contact.ReportsToId is optional on this Provider
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Put([Field.Of<Contact>(x => x.ReportsToId), Field.Of<Contact>(x => x.Department)], "Exec");

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Contact manager = (Contact)bundle.GetBundle<Contact>(x => x.ReportsToId)!.PrimaryRecords()![0];
        Assert.Equal("Exec", manager.Department);
    }

    // Loud errors --------------------------------------------------

    [Fact]
    public void Put_WhenAPathFieldIsNotARelationship_Throws()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .Put([Field.Of<Contact>(x => x.FirstName), Field.Of<Account>(x => x.Industry)], "x");

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.SupplyBundle);

        // Assert - a non-relationship path field is a loud error, not a silent no-op
        Assert.Contains("relationship", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Put_WhenThePathTargetsASharedAncestor_Throws()
    {
        // Arrange
        _ = SharedAncestor.Put(SharedAcctName, new Account { Name = "Shared HQ" });
        RecordProvider provider = new RecordProvider(typeof(Contact), SharedParentLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .Put([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Industry)], "Aerospace");

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.SupplyBundle);

        // Assert - a path value into a shared ancestor is a loud error, not a dropped value
        Assert.Contains("shared ancestor", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Put_WhenThePathHasNoRelationshipHop_Throws()
    {
        // Arrange
        RecordProvider provider = new(typeof(Contact), Lookup());

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => provider.Put([Field.Of<Account>(x => x.Industry)], "x"));

        // Assert - a one-element path has no relationship to walk
        Assert.Contains("at least one relationship", thrown.Message);
    }
}

// In-test Providers ------------------------------------------

file sealed class ContactWithOptionalManagerProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Contact>(x => x.Id))
        .Put<Contact>(x => x.LastName, new IncrementingStringExpression("Contact"))
        .Put<Contact>(x => x.Email, new UniqueEmailExpression("test.contact"))
        .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account()))
        .PutOptional<Contact>(x => x.ReportsToId, new DefaultRelationship(new Contact()));

    public PropertyInfo PrimaryTargetField => Field.Of<Contact>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}

file sealed class ContactUnderSharedAccountProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Contact>(x => x.Id))
        .Put<Contact>(x => x.LastName, new IncrementingStringExpression("Contact"))
        .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get("path-value-test-shared-acct"));

    public PropertyInfo PrimaryTargetField => Field.Of<Contact>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}

/// <summary>An Account that generates its own optional parent (self-referencing ParentId) - only needed for the deep-two-hop test.</summary>
file sealed class AccountWithOptionalParentProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Account>(x => x.Id))
        .Put<Account>(x => x.Name, new IncrementingStringExpression("Account"))
        .PutOptional<Account>(x => x.ParentId, new DefaultRelationship(new Account()));

    public PropertyInfo PrimaryTargetField => Field.Of<Account>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}

/// <summary>A User that requires a Manager generated by nextKey.</summary>
file sealed class ChainedUserProvider : IRecordProvider
{
    private MasterTemplate _template { get; }

    public ChainedUserProvider(ILookupKey nextKey) =>
        this._template = LeafUserTemplate()
            .PutRequired<User>(x => x.ManagerId, new DefaultRelationship(nextKey, new User()));

    public PropertyInfo PrimaryTargetField => Field.Of<User>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);

    internal static MasterTemplate LeafUserTemplate() =>
        new MasterTemplate(Field.Of<User>(x => x.Id))
            .Put<User>(x => x.LastName, new IncrementingStringExpression("User"))
            .Put<User>(x => x.Email, new UniqueEmailExpression("test.user"));
}

file sealed class LeafUserProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = ChainedUserProvider.LeafUserTemplate();

    public PropertyInfo PrimaryTargetField => Field.Of<User>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
