using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;

namespace Net.Nowhereatall.Xfty.Test.Relationships;

/// <summary>
/// Proves <see cref="SharedAncestor.ResetAllForTesting"/> - the test-hygiene
/// API the rest of this port's own SharedAncestor tests deliberately do NOT
/// use (they stay isolated the older way: a never-reused name per test, plus
/// an explicit <see cref="SharedAncestor.Disable(string)"/> for the couple
/// that would otherwise leak). This file proves the newer, simpler
/// alternative actually works, including the one thing the older approach
/// could never make safe: testing <see cref="SharedAncestor.ManualResolutionOnly"/>,
/// which has no unsetter of its own.
/// </summary>
public class SharedAncestorResetTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    public SharedAncestorResetTest() => SharedAncestor.ResetAllForTesting();

    [Fact]
    public void ResetAllForTesting_ClearsTheRegistry_SoANameCanBeReusedWithADifferentRecord()
    {
        // Arrange - resolve "hq" once, then reset, then register a different record under the same name
        const string name = "reset-test-registry";
        _ = SharedAncestor.Put(name, new Account { Name = "First" });
        _ = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Act
        SharedAncestor.ResetAllForTesting();
        Account replacement = new() { Name = "Second", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put(name, replacement);

        // Assert - the name resolves to the new record, with no trace of the first
        Assert.Equal(replacement.Id, SharedAncestor.GetId(name));
    }

    [Fact]
    public void ResetAllForTesting_ClearsDisabledNames()
    {
        // Arrange - disable a name, then reset
        const string name = "reset-test-disabled";
        _ = SharedAncestor.Put(name, new Account { Name = "Placeholder" });
        SharedAncestor.Disable(name);

        // Act
        SharedAncestor.ResetAllForTesting();
        _ = SharedAncestor.Put(name, new Account { Name = "Reused" });
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - the name resolves normally; "disabled" did not survive the reset
        Assert.NotNull(result.AccountId);
    }

    [Fact]
    public void ResetAllForTesting_ClearsManualResolutionOnly_MakingItSafeToTestHere()
    {
        // Arrange - turn on manual-resolution mode
        SharedAncestor.ManualResolutionOnly();
        Assert.True(SharedAncestor.IsManualResolutionOnly());

        // A lightweight shared ancestor still resolves on demand under manual mode
        const string lightweightName = "reset-test-manual-lightweight";
        _ = SharedAncestor.Put(lightweightName, new Account { Name = "Lightweight HQ" });
        Contact lightweightResult = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(lightweightName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();
        Assert.NotNull(lightweightResult.AccountId);

        // A shared ancestor with its own sub-graph is NOT auto-resolved under manual mode
        const string heavyName = "reset-test-manual-heavy";
        _ = SharedAncestor.Put(heavyName, new Account { Name = "Heavy HQ" })
            .PutRequired<Account>(x => x.ParentId, new DefaultRelationship(new Account()));
        RecordProvider heavyProvider = new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(heavyName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(heavyProvider.Supply);
        Assert.Contains("manual resolution only", thrown.Message);

        // Act - reset
        SharedAncestor.ResetAllForTesting();

        // Assert - manual mode is off, and ordinary auto-resolution works again
        Assert.False(SharedAncestor.IsManualResolutionOnly());
        const string afterResetName = "reset-test-manual-after-reset";
        _ = SharedAncestor.Put(afterResetName, new Account { Name = "Auto Again" })
            .PutRequired<Account>(x => x.ParentId, new DefaultRelationship(new Account()));
        Contact afterResetResult = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(afterResetName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();
        Assert.NotNull(afterResetResult.AccountId);
    }
}
