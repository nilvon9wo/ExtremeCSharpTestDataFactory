using System.Reflection;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Demo;

/// <summary>A mechanical port of Apex's XFTY_DefaultContactDataProvider - AccountId is a required relationship to a generated <see cref="Account"/>.</summary>
public sealed class ContactDataProvider : IRecordProvider
{
    public const string DefaultFirstNamePrefix = "Contact First Name";
    public const string DefaultLastNamePrefix = "Contact Last Name";
    public const string DefaultEmailPrefix = "test.contact";
    public const string DefaultAccountDescription = "Account for contact";

    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
        .PutRequired(
            Field.Of<Contact>(nameof(Contact.AccountId)),
            new DefaultRelationship(new Account { Description = DefaultAccountDescription }))
        .Put(Field.Of<Contact>(nameof(Contact.Email)), new UniqueEmailExpression(DefaultEmailPrefix))
        .Put(Field.Of<Contact>(nameof(Contact.FirstName)), new IncrementingStringExpression(DefaultFirstNamePrefix))
        .Put(Field.Of<Contact>(nameof(Contact.LastName)), new IncrementingStringExpression(DefaultLastNamePrefix));

    public PropertyInfo PrimaryTargetField => Field.Of<Contact>(nameof(Contact.Id));

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
