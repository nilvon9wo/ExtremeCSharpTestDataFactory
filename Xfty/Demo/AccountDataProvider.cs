using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Values;
namespace Net.NowhereAtAll.Xfty.Demo;

/// <summary>The bundled Account Provider - a starter-kit example of a declarative Master Template.</summary>
public sealed class AccountDataProvider : IRecordProvider
{
    public const string DefaultNamePrefix = "Test Account Name";
    public const string DefaultIndustry = "Test Account Industry";
    public const string DefaultType = "Test Account Type";

    public const string DefaultShippingCity = "Test Shipping City Industry";
    public const string DefaultShippingCountry = "Germany";
    public const string DefaultShippingStreet = "Test Shipping Street Industry";

    private MasterTemplate _template { get; } = new MasterTemplate<Account>(x => x.Id)
    {
        [x => x.Name] = new IncrementingStringExpression(DefaultNamePrefix),
        [x => x.Industry] = new LiteralExpression(DefaultIndustry),
        [x => x.ShippingStreet] = new LiteralExpression(DefaultShippingStreet),
        [x => x.ShippingCity] = new LiteralExpression(DefaultShippingCity),
        [x => x.ShippingCountry] = new LiteralExpression(DefaultShippingCountry),
        [x => x.Type] = new LiteralExpression(DefaultType),
    };

    public PropertyInfo PrimaryTargetField => Field.Of<Account>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
