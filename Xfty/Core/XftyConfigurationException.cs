namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>
/// Thrown when Xfty is misconfigured by its caller - a missing predicate list,
/// a null value where one is required, and similar. The framework must never
/// make a consumer debug it: this always names the misconfiguration and, where
/// possible, the fix, rather than surfacing a silent default or an opaque
/// downstream error.
/// </summary>
public class XftyConfigurationException(string message) : Exception(message)
{
}
