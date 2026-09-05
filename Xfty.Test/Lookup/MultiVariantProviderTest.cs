using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Lookup;

/// <summary>Proves multi-variant Provider resolution end to end: one record type, several Providers, chosen by lookup key. Mock mode throughout.</summary>
public class MultiVariantProviderTest
{
    // Shared keys - in a real project these live in a *LookupKeys constants class
    // that both the Provider Lookup and the pinning relationships reference.
    private static readonly ILookupKey Enterprise = FlavouredLookupKey.Get(typeof(Account), "enterprise");
    private static readonly ILookupKey Smb = FlavouredLookupKey.Get(typeof(Account), "smb");

    private static IProviderLookup NewLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new NamedAccountProvider("SMB"),
            [Enterprise] = new NamedAccountProvider("Enterprise"),
            [Smb] = new NamedAccountProvider("SMB"),
            [LookupKey.Get(typeof(Contact))] = new EnterpriseParentedContactProvider(Enterprise),
        });

    // lookup.Get(key) ------------------------------------------------

    [Fact]
    public void Get_ForAnExplicitEnterpriseKey_ReturnsTheEnterpriseProvider() => AssertGetIndustry(Enterprise, "Enterprise");

    [Fact]
    public void Get_ForAnExplicitSmbKey_ReturnsTheSmbProvider() => AssertGetIndustry(Smb, "SMB");

    [Fact]
    public void Get_ForThePlainTypeKey_ReturnsTheDefaultProvider() => AssertGetIndustry(LookupKey.Get(typeof(Account)), "SMB");

    // Variant chosen while generating a related record ---------------

    [Fact]
    public void SupplyBundle_WhenTheProvidersRelationshipPinsAVariantByKey_GeneratesThatVariant()
    {
        // Arrange - EnterpriseParentedContactProvider's required Account relationship pins 'enterprise'
        RecordProvider provider = new RecordProvider(typeof(Contact), NewLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Assert.Equal("Enterprise", ((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Industry);
    }

    [Fact]
    public void SupplyBundle_WhenAPerCallRelationshipPinsADifferentVariant_GeneratesThatVariant()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), NewLookup())
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(Smb, new Account()))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Assert.Equal("SMB", ((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Industry);
    }

    [Fact]
    public void SupplyBundle_WhenAPerCallRelationshipCarriesNoKey_GeneratesTheDefaultVariant()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), NewLookup())
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account()))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert - the plain-key Provider
        Assert.Equal("SMB", ((Account)bundle.GetList<Contact>(x => x.AccountId)![0]).Industry);
    }

    // Runner -------------------------------------------------------

    private static void AssertGetIndustry(ILookupKey key, string expectedIndustry)
    {
        // Arrange
        RecordProvider provider = new RecordProvider(key, NewLookup()).SetInsertMode(InsertMode.Mock);

        // Act
        Account result = (Account)provider.Supply();

        // Assert
        Assert.Equal(expectedIndustry, result.Industry);
    }
}

file abstract class BaseProvider : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class NamedAccountProvider : BaseProvider
{
    public NamedAccountProvider(string industry) =>
        this.Template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new IncrementingStringExpression($"{industry} Account"))
            .Put<Account>(x => x.Industry, industry);
}

file sealed class EnterpriseParentedContactProvider : BaseProvider
{
    public EnterpriseParentedContactProvider(ILookupKey enterpriseKey) =>
        this.Template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .Put<Contact>(x => x.LastName, new IncrementingStringExpression("Variant Contact"))
            // The relationship pins the same shared key the lookup registers.
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(enterpriseKey, new Account()));
}
