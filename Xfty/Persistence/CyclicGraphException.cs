namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>The lookups leave no order in which every parent lands before its child.</summary>
public sealed class CyclicGraphException(string message) : Exception(message)
{
}
