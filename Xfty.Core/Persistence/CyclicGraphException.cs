namespace Net.Nowhereatall.Xfty.Core.Persistence;

/// <summary>The lookups leave no order in which every parent lands before its child.</summary>
public sealed class CyclicGraphException : Exception
{
    public CyclicGraphException(string message) : base(message)
    {
    }
}
