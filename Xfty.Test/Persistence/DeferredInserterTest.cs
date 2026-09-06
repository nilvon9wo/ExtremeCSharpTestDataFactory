using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Test.Persistence;

/// <summary>
/// Proves the DEFERRED insert mode's registry, DeferredInserter: Register()
/// accumulates every generated-but-unsaved graph, and Flush() would save the
/// whole set in one depth-batched pass - this port has no persistence layer,
/// so Flush() always throws NotSupportedException instead of succeeding
/// (proven elsewhere too, e.g. RecordProviderIntegrationTest). What is
/// genuinely new and testable here: PendingCount() actually accumulates
/// across separate Register() calls, and a failed Flush() does not silently
/// lose the registered records (the buffer swap only happens after a
/// successful insert).
///
/// DeferredInserter's buffer is static and never successfully clears in this
/// port (Flush() always throws before the swap) - every assertion below
/// compares a before/after delta rather than an absolute count, so it stays
/// correct regardless of what earlier tests in the same process registered.
/// </summary>
public class DeferredInserterTest
{
    [Fact]
    public void PendingCount_AfterOneRegister_AccumulatesAllOfThatBundlesRecords()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact { LastName = "A" }, new Contact { LastName = "B" }]);
        int before = DeferredInserter.PendingCount();

        // Act
        DeferredInserter.Register(bundle);

        // Assert
        Assert.Equal(2, DeferredInserter.PendingCount() - before);
    }

    [Fact]
    public void PendingCount_AfterTwoRegisters_AccumulatesBoth()
    {
        // Arrange
        Bundle first = new();
        first.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account { Name = "A" }]);
        Bundle second = new();
        second.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account { Name = "B" }, new Account { Name = "C" }]);
        int before = DeferredInserter.PendingCount();

        // Act
        DeferredInserter.Register(first);
        DeferredInserter.Register(second);

        // Assert
        Assert.Equal(3, DeferredInserter.PendingCount() - before);
    }

    [Fact]
    public async Task Flush_Throws_AndDoesNotSilentlyLoseTheRegisteredRecords()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account { Name = "Not Yet Saved" }]);
        DeferredInserter.Register(bundle);
        int beforeFlush = DeferredInserter.PendingCount();

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(() => DeferredInserter.Flush());

        // Assert
        Assert.Contains("persistence gateway", thrown.Message);
        // the buffer swap only happens after a successful insert, so a failed Flush() keeps everything pending
        Assert.Equal(beforeFlush, DeferredInserter.PendingCount());
    }
}
