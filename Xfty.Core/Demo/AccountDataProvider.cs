using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core.Demo;

/// <summary>A minimal demo <see cref="IRecordProvider"/> for <see cref="Account"/> - Name is an incrementing string, everything else defaults to null.</summary>
public sealed class AccountDataProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
        .Put(Field.Of<Account>(nameof(Account.Name)), new IncrementingStringExpression("Account"));

    public PropertyInfo PrimaryTargetField => Field.Of<Account>(nameof(Account.Id));

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
