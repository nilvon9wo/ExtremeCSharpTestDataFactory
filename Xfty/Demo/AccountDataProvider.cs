using System.Reflection;
using Net.Nowhereatall.Xfty.Values;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Demo;

/// <summary>
/// The bundled Account Provider - a mechanical port of Apex's
/// XFTY_DefaultAccountDataProvider. Record-type / Person-Account variants
/// have no C# analog (see csharp-port-idea.md's RecordType carve-out) and
/// are not attempted here.
/// </summary>
public sealed class AccountDataProvider : IRecordProvider
{
    public const string DefaultNamePrefix = "Test Account Name";
    public const string DefaultIndustry = "Test Account Industry";
    public const string DefaultType = "Test Account Type";

    public const string DefaultShippingCity = "Test Shipping City Industry";
    public const string DefaultShippingCountry = "Germany";
    public const string DefaultShippingStreet = "Test Shipping Street Industry";

    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
        .Put(Field.Of<Account>(nameof(Account.Name)), new IncrementingStringExpression(DefaultNamePrefix))
        .Put(Field.Of<Account>(nameof(Account.Industry)), new LiteralExpression(DefaultIndustry))
        .Put(Field.Of<Account>(nameof(Account.ShippingStreet)), new LiteralExpression(DefaultShippingStreet))
        .Put(Field.Of<Account>(nameof(Account.ShippingCity)), new LiteralExpression(DefaultShippingCity))
        .Put(Field.Of<Account>(nameof(Account.ShippingCountry)), new LiteralExpression(DefaultShippingCountry))
        .Put(Field.Of<Account>(nameof(Account.Type)), new LiteralExpression(DefaultType));

    public PropertyInfo PrimaryTargetField => Field.Of<Account>(nameof(Account.Id));

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
