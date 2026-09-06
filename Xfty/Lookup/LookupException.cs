using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Engine;
namespace Net.NowhereAtAll.Xfty.Lookup;

public sealed class LookupException(string message) : XftyConfigurationException(message)
{
}
