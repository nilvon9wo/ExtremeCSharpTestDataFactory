using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.Children;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves every fluent method on <see cref="ChildProvider{TChild}"/> forwards to
/// the identically-named method on the wrapped plain <see cref="ChildProvider"/>
/// with its arguments intact, and returns the wrapper so the chain stays typed.
/// One test per forwarder (or forwarder pair), organised by the partial file it
/// lives in. The plain <see cref="ChildProvider"/>'s own semantics are proven in
/// <see cref="ChildProviderTest"/> - these tests only prove the delegation.
/// </summary>
public class ChildProviderOfTForwardingTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    // FieldConfig ------------------------------------------------------

    [Fact]
    public async Task Put_ByPropertyInfo_ForwardsValueExpressionsLiteralsAndContextAwareExpressions()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId)
                .Put(Field.Of<Contact>(x => x.FirstName), new LiteralExpression("Alice"))
                .Put(Field.Of<Contact>(x => x.LastName), (object?)"Smith")
                .Put(Field.Of<Contact>(x => x.Department), CopyFromSiblingExpression.From<Contact>(x => x.FirstName)))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.Equal("Alice", child.FirstName);
        Assert.Equal("Smith", child.LastName);
        Assert.Equal("Alice", child.Department); // the context-aware expression copied FirstName
    }

    [Fact]
    public async Task Put_ByLambda_ForwardsValueExpressionsLiteralsAndContextAwareExpressions()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId)
                .Put(x => x.FirstName, new LiteralExpression("Bob"))
                .Put(x => x.LastName, "Jones")
                .Put(x => x.Department, CopyFromSiblingExpression.From<Contact>(x => x.LastName)))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.Equal("Bob", child.FirstName);
        Assert.Equal("Jones", child.LastName);
        Assert.Equal("Jones", child.Department);
    }

    [Fact]
    public async Task PutRequired_ByPropertyInfo_GeneratesTheChildsOwnRequiredParent()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId)
                .PutRequired(Field.Of<Contact>(x => x.ReportsToId), new DefaultRelationship(new Contact())))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.NotNull(child.ReportsToId);
    }

    [Fact]
    public async Task PutOptional_ByPropertyInfo_IsSkippedAtRequiredInclusivity()
    {
        // Arrange - proves the forwarder is not aliased to PutRequired
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId)
                .PutOptional(Field.Of<Contact>(x => x.ReportsToId), new DefaultRelationship(new Contact())))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.Null(child.ReportsToId);
    }

    [Fact]
    public async Task PutRequired_ByLambda_GeneratesTheChildsOwnRequiredParent()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId)
                .PutRequired(x => x.ReportsToId, new DefaultRelationship(new Contact())))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.NotNull(child.ReportsToId);
    }

    [Fact]
    public async Task PutOptional_ByLambda_IsSkippedAtRequiredInclusivity()
    {
        // Arrange
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId)
                .PutOptional(x => x.ReportsToId, new DefaultRelationship(new Contact())))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.Null(child.ReportsToId);
    }

    // Setters --------------------------------------------------------

    [Fact]
    public async Task SetInclusivity_ForwardsToTheChildsOwnRelationships()
    {
        // Arrange - the child opts its own optional relationship in, above the parent's Required
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId)
                .PutOptional(x => x.ReportsToId, new DefaultRelationship(new Contact()))
                .SetInclusivity(InsertInclusivity.All))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.NotNull(child.ReportsToId); // the child's own All inclusivity generated the optional parent
    }

    [Fact]
    public async Task WithVariant_PinsTheChildProviderVariant()
    {
        // Arrange
        ILookupKey enterprise = FlavouredLookupKey.Get<Contact>("enterprise");
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Account>()] = new AccountDataProvider(),
            [LookupKey.Get<Contact>()] = new NamedLastNameContactProvider("Default"),
            [enterprise] = new NamedLastNameContactProvider("Enterprise"),
        });
        RecordProvider<Account> provider = new RecordProvider<Account>(lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId).WithVariant(enterprise))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.Equal("Enterprise", child.LastName);
    }

    [Fact]
    public async Task With_NestsAGrandchildCollectionUnderTheTypedChild()
    {
        // Arrange - grandchild Contacts hung off each child Contact by ReportsToId
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup)
            .With(new ChildProvider<Contact>(x => x.AccountId).SetQuantity(2)
                .With(ChildProvider.For<Contact>(x => x.ReportsToId).SetQuantity(3)))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        List<object> grandchildren = bundle.GetChildBundle<Contact>(x => x.AccountId)!.GetChildList<Contact>(x => x.ReportsToId);
        Assert.Equal(6, grandchildren.Count); // 2 child Contacts x 3 grandchildren
    }
}

file sealed class NamedLastNameContactProvider : IRecordProvider
{
    private MasterTemplate _template { get; }

    public NamedLastNameContactProvider(string lastName) =>
        this._template = new MasterTemplate<Contact>(x => x.Id)
        {
            [x => x.LastName] = new LiteralExpression(lastName),
        };

    public PropertyInfo PrimaryTargetField => this._template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this._template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
