using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Xunit.Test;

/// <summary>
/// The negative case <see cref="IsolatesSharedAncestorAttribute"/> exists to
/// prevent - proof that the leak is real, not just a theoretical risk the
/// positive tests take on faith.
///
/// This is deliberately **one** test method simulating two, rather than two
/// real xUnit test methods relying on a particular run order between them.
/// <see cref="SharedAncestor"/>'s registry has no concept of "which test
/// method is running" - it only ever sees a sequence of Put/Disable/resolve
/// calls - so two such sequences back to back in one method body reproduce
/// the exact same static state a real two-test scenario would, without
/// depending on xUnit's (unguaranteed) method-ordering to prove it.
///
/// The scenario is specifically <see cref="SharedAncestor.Disable(string)"/>,
/// not a second <see cref="SharedAncestor.Put(string, object)"/> under the
/// same name - `Put` replacing an already-registered name is documented,
/// supported behavior (see `SharedAncestorTest.Put_OverAResolvedSharedAncestorWithADifferentRecord_Succeeds`),
/// not a bug. `Disable` has no such override: once a name is disabled,
/// nothing short of a reset un-disables it, which is exactly what makes it
/// a clean, unambiguous demonstration of real contamination.
/// </summary>
public class SharedAncestorLeaksWithoutIsolationTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public async Task WithoutIsolation_DisablingAnAncestorContaminatesAnUnrelatedLaterUseOfTheSameName()
    {
        const string sharedName = "leak-demo-disable";

        // "Logical test A" - deliberately disables this name (a null-FK scenario, say) and moves on
        _ = SharedAncestor.Put(sharedName, new Account());
        SharedAncestor.Disable(sharedName);

        // "Logical test B" - has no idea Test A ever ran; registers its own, unrelated record under the same name
        Account fromLogicalTestB = new() { Name = "B", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put(sharedName, fromLogicalTestB);
        Contact result = (Contact)await new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(sharedName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - Test B's own, freshly-registered record is silently discarded because of Test A's
        // unrelated Disable() call: the FK resolves to null instead of fromLogicalTestB's Id, and asking
        // directly throws "disabled" - a real, observable bug this test's own name has never been through
        // SharedAncestor.ResetAllForTesting(), unlike every test in IsolatesSharedAncestorAttributeTest.
        Assert.Null(result.AccountId);
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => SharedAncestor.GetId(sharedName));
        Assert.Contains("disabled", thrown.Message);

        // Cleanup - so this file's own next test run (or others sharing this process) don't inherit it either
        SharedAncestor.ResetAllForTesting();
    }
}
