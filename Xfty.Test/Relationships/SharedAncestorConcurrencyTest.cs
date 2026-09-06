using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Test.Relationships;

/// <summary>
/// Proves <see cref="SharedAncestor"/> is safe under genuine concurrent
/// access - not a theoretical concern: before the fix, the scenario below
/// (many distinct names registered and resolved at once) reliably crashed
/// with "A concurrent update was performed on this collection and
/// corrupted its state," surfaced by running a new test project without
/// this repo's own xunit.runner.json opt-out (xUnit's *default* behaviour
/// is to run different test classes in parallel). PLINQ manufactures the
/// race directly here, independent of any test-runner scheduling, so this
/// stays a reliable regression test regardless of collection-parallelism
/// settings in this or any other project.
/// </summary>
public class SharedAncestorConcurrencyTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void ConcurrentAccess_AcrossManyDistinctNames_DoesNotCorruptTheRegistry()
    {
        // Arrange
        const int attemptCount = 200;
        IProviderLookup lookup = Lookup();

        // Act - every index uses its own never-colliding name, all racing the shared static registry at once
        List<Exception> failures = [.. Enumerable.Range(0, attemptCount)
            .AsParallel()
            .Select(index => TryPutAndSupply(lookup, index))
            .OfType<Exception>()];

        // Assert - no attempt corrupted the shared registry
        Assert.Empty(failures);
    }

    [Fact]
    public void ConcurrentAccess_ToTheSameName_ResolvesToExactlyOneRecordForEveryCaller()
    {
        // Arrange
        const string name = "concurrency-test-shared-name";
        const int callerCount = 200;
        IProviderLookup lookup = Lookup();
        _ = SharedAncestor.Put(name, new Account { Name = "Shared" });

        // Act - every caller races to resolve the SAME shared ancestor at once
        List<string?> accountIds = [.. Enumerable.Range(0, callerCount)
            .AsParallel()
            .Select(_ => SupplyOneContact(lookup, name))];

        // Assert - every caller saw the one, single resolution - not a duplicate or partially-built one
        _ = Assert.Single(accountIds.Distinct());
        Assert.All(accountIds, Assert.NotNull);
    }

    private static Exception? TryPutAndSupply(IProviderLookup lookup, int index)
    {
        try
        {
            string name = $"concurrency-test-distinct-{index}";
            _ = SharedAncestor.Put(name, new Account { Name = $"Concurrent {index}" });
            _ = SupplyOneContact(lookup, name);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    // PLINQ needs these blocking (not awaited) - the whole point of this test is
    // forcing the race across real OS threads, independent of how the thread pool
    // happens to schedule async continuations. Same sync-over-async bridge (and
    // the same caveat) as XftySpecimenBuilder/XftyAutoBogusOverride: safe here
    // because xUnit's test threads carry no captured SynchronizationContext.
    private static string? SupplyOneContact(IProviderLookup lookup, string name) =>
        ((Contact)new RecordProvider(typeof(Contact), lookup)
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply().GetAwaiter().GetResult()).AccountId;
}
