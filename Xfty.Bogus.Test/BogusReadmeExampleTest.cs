using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Bogus.Test;

/// <summary>
/// Proves the exact usage shown in this package's own README.md and
/// docs/use/bogus.md - a Master Template entry wired straight to a bundled
/// Bogus expression, not just the expression in isolation (see
/// FakeFullNameExpressionTest and its siblings for that).
/// </summary>
file sealed class ContactWithFakeDataProvider() : SimpleRecordProvider<Contact>(
    new MasterTemplate<Contact>(x => x.Id)
    {
        [x => x.FirstName] = new FakeFullNameExpression(),
        [x => x.Email] = new FakeEmailAddressExpression(),
    })
{
}

public class BogusReadmeExampleTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Contact))] = new ContactWithFakeDataProvider(),
        });

    [Fact]
    public async Task Supply_UsingBogusExpressionsInAMasterTemplate_ProducesRealisticLookingFields()
    {
        // Arrange
        IProviderLookup lookup = Lookup();

        // Act
        Contact result = (Contact)await new RecordProvider(typeof(Contact), lookup).Supply();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result.FirstName));
        Assert.Contains('@', result.Email!);
    }
}
