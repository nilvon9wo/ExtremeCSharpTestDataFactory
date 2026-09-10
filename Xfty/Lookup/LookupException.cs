using Net.NowhereAtAll.Xfty.Core;
namespace Net.NowhereAtAll.Xfty.Lookup;

public sealed class LookupException(string message) : XftyConfigurationException(message)
{
}