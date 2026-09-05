using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Persistence;
using Net.Nowhereatall.Xfty.Relationships;

namespace Net.Nowhereatall.Xfty.Xunit.Test;

/// <summary>
/// Proves <see cref="IsolatesSharedAncestorAttribute"/> actually prevents
/// leakage between test methods that reuse the same shared-ancestor name -
/// the exact scenario a consumer would otherwise have to avoid with unique
/// names or their own manual `SharedAncestor.ResetAllForTesting()` wiring.
/// Both tests below deliberately share one name; either could run first,
/// and each must still see only its own registration.
/// </summary>
[IsolatesSharedAncestor]
public class IsolatesSharedAncestorAttributeTest
{
    private const string SharedName = "isolation-test-shared-name";

    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void FirstTest_RegistersAndResolvesItsOwnRecord()
    {
        // Arrange - a fixed Id makes this a value Put, not a template, so the record itself is the resolved one
        Account first = new() { Name = "First", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put(SharedName, first);

        // Act
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(SharedName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert
        Assert.Equal(first.Id, result.AccountId);
    }

    [Fact]
    public void SecondTest_ReusingTheSameName_SeesOnlyItsOwnRecord()
    {
        // Arrange - same name as FirstTest; would collide with its resolution without isolation
        Account second = new() { Name = "Second", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put(SharedName, second);

        // Act
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(SharedName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert - resolves to THIS test's own record, regardless of run order relative to FirstTest
        Assert.Equal(second.Id, result.AccountId);
    }
}

/// <summary>Proves the attribute works applied directly to one method, not just a whole class.</summary>
public class IsolatesSharedAncestorAttributeMethodLevelTest
{
    private const string SharedName = "isolation-test-method-level-name";

    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    [IsolatesSharedAncestor]
    public void IsolatedTest_RegistersAndResolvesCleanly()
    {
        // Arrange - a fixed Id makes this a value Put, not a template, so the record itself is the resolved one
        Account account = new() { Name = "Method-Level", Id = IdMocker.GenerateId() };
        _ = SharedAncestor.Put(SharedName, account);

        // Act
        Contact result = (Contact)new RecordProvider(typeof(Contact), Lookup())
            .PutRequired<Contact>(x => x.AccountId, SharedAncestor.Get(SharedName))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply();

        // Assert
        Assert.Equal(account.Id, result.AccountId);
    }
}
