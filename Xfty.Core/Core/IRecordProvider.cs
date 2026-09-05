using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core.Core;

public interface IRecordProvider
{
    PropertyInfo PrimaryTargetField { get; }

    MasterTemplate MasterTemplate { get; }

    Bundle CreateBundle(GenerationContext context, List<object> templateRecords);
}
