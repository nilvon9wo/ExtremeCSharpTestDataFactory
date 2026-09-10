using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;

namespace Net.NowhereAtAll.Xfty.Core.RecordProviders;

public interface IRecordProvider
{
    PropertyInfo PrimaryTargetField { get; }

    MasterTemplate MasterTemplate { get; }

    Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords);
}