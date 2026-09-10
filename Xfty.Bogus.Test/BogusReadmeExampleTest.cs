using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Bogus.Test;

/// <summary>
/// Proves the exact usage shown in this package's own README.md and
/// docs/use/bogus.md - a Master Template entry wired straight to a bundled
/// Bogus expression, not just the expression in isolation (see
/// FakeFullNameExpressionTest and its siblings for that).
/// </summary>
file sealed class ContactWithFakeDataProvider : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<Contact>(x => x.Id)
    {
        [x => x.FirstName] = new FakeFullNameExpression(),
        [x => x.Email] = new FakeEmailAddressExpression(),
    };

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}

public class BogusReadmeExampleTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Contact>()] = new ContactWithFakeDataProvider(),
        });

    [Fact]
    public async Task Supply_UsingBogusExpressionsInAMasterTemplate_ProducesRealisticLookingFields()
    {
        // Arrange
        IProviderLookup lookup = Lookup();

        // Act
        Contact result = (Contact)await new RecordProvider(typeof(Contact), lookup).Supply().ConfigureAwait(true);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result.FirstName));
        Assert.Contains('@', result.Email!);
    }
}