using System.Reflection;
using Net.Nowhereatall.Xfty.Engine;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>
/// Base for a Provider that is nothing more than a Master Template - by far
/// the common case. Implements the <see cref="IRecordProvider"/> plumbing
/// (PrimaryTargetField, CreateBundle) so a Provider definition is just its
/// template:
///
/// <code>
/// file sealed class ContactUnderAccountProvider()
///     : SimpleRecordProvider&lt;Contact&gt;(
///         new MasterTemplate&lt;Contact&gt;(x => x.Id)
///             .PutRequired(x => x.AccountId, new DefaultRelationship(new Account())));
/// </code>
///
/// Write a Provider directly against <see cref="IRecordProvider"/> instead
/// when <see cref="CreateBundle"/> needs custom behaviour beyond "build the
/// template".
/// </summary>
public abstract class SimpleRecordProvider<TRecord>(MasterTemplate<TRecord> template) : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = template;

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}
