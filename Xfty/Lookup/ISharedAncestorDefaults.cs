namespace Net.Nowhereatall.Xfty.Lookup;

/// <summary>
/// Optional companion to <see cref="IProviderLookup"/>. A project whose
/// Providers reference shared ancestors implements this on its lookup too, so
/// those shared ancestors have a default configuration and the Providers work
/// without every test registering them by hand.
/// </summary>
public interface ISharedAncestorDefaults
{
    void RegisterSharedAncestorDefaults();
}
