using global::AutoFixture.Kernel;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.AutoFixture;

/// <summary>
/// Intercepts a request for a type with a registered <see cref="IRecordProvider"/>
/// and answers it with <c>new RecordProvider(type, lookup).SetInsertMode(insertMode).Supply()</c>
/// instead of AutoFixture's own generation - so <c>fixture.Create&lt;Contact&gt;()</c>
/// gets a fully-formed XFTY graph (required relationships resolved, shared
/// ancestors deduplicated, cycle detection intact) rather than AutoFixture's
/// own recursive auto-property population, which knows nothing about any of
/// that. Anything requested that XFTY has no Provider for falls through to
/// <see cref="NoSpecimen"/>, letting the rest of the pipeline (including
/// AutoFixture's own default generation) handle it as usual.
///
/// Registered via <see cref="XftyCustomization"/>, not used directly.
/// </summary>
public sealed class XftySpecimenBuilder(IProviderLookup lookup, InsertMode insertMode, InsertInclusivity inclusivity) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context) =>
        request is Type type && this.IsRegistered(type)
            ? new RecordProvider(type, lookup).SetInsertMode(insertMode).SetInclusivity(inclusivity).Supply()
            : new NoSpecimen();

    private bool IsRegistered(Type type)
    {
        try
        {
            _ = lookup.Get(type);
            return true;
        }
        catch (LookupException)
        {
            return false;
        }
    }
}
