using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Relationships;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Demo;

/// <summary>A minimal demo <see cref="IRecordProvider"/> for <see cref="Contact"/> - LastName is an incrementing string, AccountId is a required relationship to a generated <see cref="Account"/>.</summary>
public sealed class ContactDataProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
        .Put(Field.Of<Contact>(nameof(Contact.LastName)), new IncrementingStringExpression("Contact"))
        .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), new DefaultRelationship(new Account()));

    public PropertyInfo PrimaryTargetField => Field.Of<Contact>(nameof(Contact.Id));

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
