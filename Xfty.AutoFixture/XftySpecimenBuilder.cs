using global::AutoFixture.Kernel;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.AutoFixture;

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
/// AutoFixture's <see cref="ISpecimenBuilder.Create"/> is a synchronous SPI -
/// not something this package controls the shape of - so this bridges
/// sync-over-async via <c>Task.Run(...).GetAwaiter().GetResult()</c>. The
/// inner <c>Task.Run(...)</c> is deliberate, not decoration: it forces
/// XFTY's generation onto a fresh thread-pool thread with no captured
/// synchronization context at all, so the blocking wait below can never
/// deadlock waiting on a continuation that needed the very thread it's
/// blocking - regardless of what thread calls in (a UI thread, classic
/// ASP.NET's request context, anything). XFTY is itself a piece of test
/// infrastructure - a third source of failures beyond the code under test
/// and the tests themselves - so this intentionally trades a negligible
/// thread-pool hop for eliminating a whole class of hard-to-diagnose hangs,
/// rather than only documenting the risk away for the contexts already
/// known to be safe.
///
/// Registered via <see cref="XftyCustomization"/>, not used directly.
/// </summary>
public sealed class XftySpecimenBuilder(IProviderLookup lookup, InsertMode insertMode, InsertInclusivity inclusivity) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context) =>
        request is Type type && this.IsRegistered(type)
            ? Task.Run(() => new RecordProvider(type, lookup).SetInsertMode(insertMode).SetInclusivity(inclusivity).Supply()).GetAwaiter().GetResult()
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
