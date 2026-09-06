using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

public interface IRecordProvider
{
    PropertyInfo PrimaryTargetField { get; }

    MasterTemplate MasterTemplate { get; }

    Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords);
}
