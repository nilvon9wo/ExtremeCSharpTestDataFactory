namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>Thrown when a RecordProvider is given data for a record type other than the one it was constructed for.</summary>
public sealed class RecordProviderConflictException(string message) : XftyConfigurationException(message)
{
}
