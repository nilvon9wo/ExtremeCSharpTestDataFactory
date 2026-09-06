using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/deferred-insert.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExDeferredInsertTest
{
    private static readonly DefaultProviderLookup Lookup = new();

    [Fact]
    public async Task GenerateOverManyCalls_RegisterInsteadOfInserting()
    {
        // from docs/use/deferred-insert.md "Deferred - generate over many calls, register instead of inserting"
        Bundle accounts = await new RecordProvider(typeof(Account), Lookup)
            .SetInsertMode(InsertMode.Deferred)
            .SetQuantityPerTemplate(3)
            .SupplyBundle();

        Bundle contacts = await new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Deferred)
            .SupplyBundle();

        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(() => DeferredInserter.Flush());

        Assert.NotEmpty(accounts.PrimaryRecords()!);
        Assert.NotEmpty(contacts.PrimaryRecords()!);
        Assert.Contains("persistence gateway", thrown.Message);
        DeferredInserter.ResetForTesting(); // the failed Flush() deliberately left the registry non-empty
    }

    [Fact]
    public async Task InspectingTheResolvedGraphWithoutPersisting()
    {
        // from docs/use/deferred-insert.md "Inspecting the resolved graph without persisting"
        Bundle bundle = await new RecordProvider(typeof(Contact), Lookup)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Deferred)
            .SupplyBundle();

        DeferredInsertBuffer graph = DeferredInsertBuffer.Flatten(bundle);
        await graph.ResolveAll(InsertMode.Mock);

        Assert.All(graph.Records(), record => Assert.NotNull(record.GetType().GetProperty("Id")!.GetValue(record)));

        // Cleanup - SupplyBundle() under Deferred also registered this bundle into the global
        // static registry as a side effect; this test only ever inspects its own local Flatten() copy
        DeferredInserter.ResetForTesting();
    }
}
