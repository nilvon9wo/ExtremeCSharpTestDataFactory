using Net.Nowhereatall.Xfty.Core.Core;
using Net.Nowhereatall.Xfty.Core.Engine;
namespace Net.Nowhereatall.Xfty.Core.Lookup;

public sealed class LookupException : XftyConfigurationException
{
    public LookupException(string message) : base(message)
    {
    }
}
