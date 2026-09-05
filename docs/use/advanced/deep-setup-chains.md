# Building Data Across Helper Methods

> This page describes a genuinely different situation from the Apex original,
> not just a syntax translation — read the note below before assuming the Apex
> pattern carries over.

Apex's `@TestSetup` runs once per test method and is rolled back with the rest
of that method's DML; a `static` field on an Apex test class is *also*
re-initialised fresh for every test method, which is what let the Apex original
build shared fixtures as plain `static` variables.

**xUnit has neither behaviour.** By default xUnit creates a **new instance of
the test class per test method**, so ordinary (non-`static`) fields and a
constructor genuinely do run fresh for every test — that is the direct
replacement for a small `@TestSetup`. But a `static` field or a `static`
registry (this port's `SharedAncestor` and `DeferredInserter` are exactly this)
is **not** reset between test methods — it lives for the whole test run. Relying
on "static resets automatically" the way the Apex original could is actively
dangerous here; see the shared-ancestor naming/cleanup notes in
[shared-ancestors](../shared-ancestors.md) and
[reference/known-issues.md](../../reference/known-issues.md).

---

## The instance-based replacement for `@TestSetup`

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

Apex's pattern — build several bundles across helper methods, `flush()` once —
still has a genuine reason to exist here: it proves a graph spanning several
`SupplyBundle()` calls resolves and links correctly as **one** unit before any
persistence step, even though the flush itself throws in this port (see
[deferred-insert](../deferred-insert.md)).

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

    // Act / Assert - Flush() always throws in this port; PendingCount() already proves both bundles registered
    Assert.True(pendingBeforeFlush >= 12); // 3 Accounts + 9 Contacts (their own Accounts too, if Required)
    _ = Assert.Throws<NotSupportedException>(DeferredInserter.Flush);
}
```

To inspect the flattened, fully-resolved graph in memory without attempting to
persist it, use `DeferredInsertBuffer.Flatten(bundle)` per bundle, or build one
`DeferredInsertBuffer` by hand and `Add(...)` each bundle to it before calling
`ResolveAll(InsertMode.Mock)` — see [deferred-insert](../deferred-insert.md).

Runnable: `DeferredInserterTest`, `DeferredInsertBufferTest`
