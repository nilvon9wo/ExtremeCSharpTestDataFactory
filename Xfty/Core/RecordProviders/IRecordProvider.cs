using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.Bundles;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

public interface IRecordProvider
{
    PropertyInfo PrimaryTargetField { get; }

    MasterTemplate MasterTemplate { get; }

    Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords);
}
