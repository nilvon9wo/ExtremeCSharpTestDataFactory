using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;
namespace Net.NowhereAtAll.Xfty.Demo;

/// <summary>The bundled Contact Provider - AccountId is a required relationship to a generated <see cref="Account"/>.</summary>
public sealed class ContactDataProvider : IRecordProvider
{
    public const string DefaultFirstNamePrefix = "Contact First Name";
    public const string DefaultLastNamePrefix = "Contact Last Name";
    public const string DefaultEmailPrefix = "test.contact";
    public const string DefaultAccountDescription = "Account for contact";

    private MasterTemplate _template { get; } = new MasterTemplate<Contact>(x => x.Id)
    {
        [x => x.Email] = new UniqueEmailExpression(DefaultEmailPrefix),
        [x => x.FirstName] = new IncrementingStringExpression(DefaultFirstNamePrefix),
        [x => x.LastName] = new IncrementingStringExpression(DefaultLastNamePrefix),
    }.PutRequired(x => x.AccountId, new DefaultRelationship(new Account { Description = DefaultAccountDescription }));

    public PropertyInfo PrimaryTargetField => Field.Of<Contact>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
