using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Lookup;

public sealed class LookupException : XftyConfigurationException
{
    public LookupException(string message) : base(message)
    {
    }
}
