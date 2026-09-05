# Building Data Across Helper Methods

xUnit creates a **new instance of the test class for every test method**, so
an ordinary (non-`static`) field and a constructor genuinely run fresh each
time - that's the natural place for shared setup. A `static` field or
registry, by contrast, is **not** reset between test methods - it lives for
the whole test run. This port's `SharedAncestor` and `DeferredInserter` are
exactly this kind of `static` registry; relying on it resetting itself is
actively dangerous. See the naming/cleanup notes in
[shared-ancestors](../shared-ancestors.md) and
[reference/known-issues.md](../../reference/known-issues.md).

---

## Shared setup, built once per test method

Build the shared data in the constructor, or a plain instance method the
constructor calls; xUnit gives every test method its own instance, so this
already re-runs per test with no extra machinery:

<!-- sketch -->
```csharp
public class ContactValidationTests
{
    private readonly DefaultProviderLookup lookup = new();
    private readonly List<object> sharedAccounts;

    public ContactValidationTests()
    {
        this.sharedAccounts = new RecordProvider(typeof(Account), this.lookup)
            .SetInsertMode(InsertMode.Mock)
            .SetQuantityPerTemplate(3)
            .SupplyList();
    }

    [Fact]
    public void SomeTest()
    {
        // this.sharedAccounts is fresh for this test method
    }
}
```

For data shared **across every test method in a class** (built once, not
per-method — the `IClassFixture<T>` idiom), see the xUnit documentation; nothing
about that interacts with XFTY specifically.

---

## `Deferred` across several helper methods

Building several bundles across helper methods, then flushing once, proves a
graph spanning several `SupplyBundle()` calls resolves and links correctly as
**one** unit before any persistence step runs — see
[deferred-insert](../deferred-insert.md).

<!-- sketch -->
```csharp
private Bundle SeedAccounts() =>
    new RecordProvider(typeof(Account), this.lookup)
        .SetInsertMode(InsertMode.Deferred)
        .SetQuantityPerTemplate(3)
        .SupplyBundle();

private Bundle SeedContacts() =>
    new RecordProvider(typeof(Contact), this.lookup)
        .SetInclusivity(InsertInclusivity.Required)
        .SetInsertMode(InsertMode.Deferred)
        .SetQuantityPerTemplate(9)
        .SupplyBundle();

[Fact]
public void TheWholeGraphRegistersBeforeAnyFlushAttempt()
{
    // Arrange
    Bundle seededAccounts = this.SeedAccounts();
    Bundle seededContacts = this.SeedContacts();
    int pendingBeforeFlush = DeferredInserter.PendingCount();

    // Act / Assert - both bundles registered as one pending set before any flush
    Assert.True(pendingBeforeFlush >= 12); // 3 Accounts + 9 Contacts (their own Accounts too, if Required)
    DeferredInserter.Flush(gateway); // one pass, in dependency order, through the given IPersistenceGateway
}
```

To inspect the flattened, fully-resolved graph in memory without attempting to
persist it, use `DeferredInsertBuffer.Flatten(bundle)` per bundle, or build one
`DeferredInsertBuffer` by hand and `Add(...)` each bundle to it before calling
`ResolveAll(InsertMode.Mock)` — see [deferred-insert](../deferred-insert.md).

Runnable: `DeferredInserterTest`, `DeferredInsertBufferTest`
